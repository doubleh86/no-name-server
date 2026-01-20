using System.Numerics;
using CommonData.CommonModels.Enums;
using CommonData.NetworkModels.WorldServerProtocols.GameProtocols;
using DataTableLoader.Models;
using NetworkProtocols.Socket.WorldServerProtocols.Models;
using ServerFramework.CommonUtils.DateTimeHelper;

namespace WorldServer.GameObjects;

public class MonsterObject : GameObject
{
    private readonly MonsterGroup _group;
    private readonly MonsterObjectBase _packet = new();
    private readonly MonsterInfo _tableData;
    
    private readonly Vector3 _startPosition;
    private Vector3 _changePosition = Vector3.Zero;
    private float _changeRotation = 0f;

    private AIState _state;

    // ---- AI 타이밍 ----
    private DateTime _nextDecisionTime = TimeZoneHelper.UtcNow;
    private DateTime _lastMoveTime = TimeZoneHelper.UtcNow;

    // ---- Attack -> Chase 전환 튐 방지 ----
    private DateTime _chaseMoveResumeTime = DateTime.MinValue;

    // ---- 회전(서버는 degree로 통일) ----
    private float _yawDeg;

    // ===== 튜닝값 =====
    private const int DecisionMs = 200;            // 상태 결정 주기 (200ms)
    
    // 히스테리시스 (경계 튐 방지)
    private const float AttackEnter = 3f;
    private const float AttackExit  = 5f;          // Attack 풀리는 거리(Enter보다 크게)
    private const float ChaseEnter  = 25f;
    private const float ChaseExit   = 35f;         // Chase 풀리는 거리(Enter보다 크게)

    // 이동/회전
    private const float MaxTurnDegPerSec = 360f;   // 초당 최대 회전각
    private const int AttackToChaseFreezeMs = 100; // Attack->Chase 전환 후 1틱 정지

    public AIState GetState() => _state;
    public MonsterGroup GetGroup() => _group;
    
    public Vector3 GetChangePosition() => _changePosition;
    public float GetChangeRotation() => _changeRotation;



    public MonsterObject(long id, Vector3 position, int zoneId, MonsterGroup group, MonsterInfo tableData)
        : base(id, zoneId, position, GameObjectType.Monster)
    {
        _group = group;
        _tableData = tableData.Clone() as MonsterInfo;

        // 기존 rotation이 있으면 그걸 기준으로 잡기
        _yawDeg = GetRotation();
        _startPosition = position;
    }

    private void _UpdateStateChange(DateTime utcNow, AIState newState)
    {
        if (_state == newState)
            return;

        _isChanged = true;

        // Attack -> Chase로 바뀌는 순간에만 살짝 멈춰서 "툭" 튀는 거 제거
        if (_state == AIState.Attack && newState == AIState.Chase)
            _chaseMoveResumeTime = utcNow.AddMilliseconds(AttackToChaseFreezeMs);

        _state = newState;
    }

    /// <summary>
    /// 상태 결정(전이)만 담당: 200ms마다 호출
    /// 이동은 매 틱에서 별도로 처리해야 부드러움
    /// </summary>
    private void _DecideState(DateTime utcNow, Vector3 playerPosition)
    {
        var cur = GetPosition();

        // Return 상태는 이동 루프에서 도착 처리
        if (_state == AIState.Return)
            return;

        // 앵커에서 너무 멀면 복귀
        var anchorDist = Vector3.Distance(_group.AnchorPosition, cur);
        if (anchorDist > _tableData.max_anchor_distance)
        {
            _UpdateStateChange(utcNow, AIState.Return);
            return;
        }

        var dist = Vector3.Distance(playerPosition, cur);

        // ---- 히스테리시스 전이 ----

        // Attack 유지/해제
        if (_state == AIState.Attack)
        {
            if (dist > AttackExit)
                _UpdateStateChange(utcNow, AIState.Chase); // Return X, Chase O
            return;
        }

        // Chase 유지/해제 + Attack 진입
        if (_state == AIState.Chase)
        {
            if (dist < AttackEnter)
            {
                _UpdateStateChange(utcNow, AIState.Attack);
                return;
            }

            if (dist <= ChaseExit)
                return; // 계속 Chase 유지

            _UpdateStateChange(utcNow, AIState.Idle);
            return;
        }

        // Idle/Sleep 등에서 진입
        if (dist < AttackEnter)
        {
            _UpdateStateChange(utcNow, AIState.Attack);
            return;
        }

        if (dist < ChaseEnter)
        {
            _UpdateStateChange(utcNow, AIState.Chase);
            return;
        }

        _UpdateStateChange(utcNow, AIState.Idle);
    }
    
    public void ForceSetPositionAndRotation(Vector3 newPos, float yawDeg)
    {
        _UpdateChangePositionAndRotation(newPos, yawDeg);
    }

    public void ForceSetPosition(Vector3 newPos)
    {
        _UpdateChangePositionAndRotation(newPos, GetRotation());
    }


    /// <summary>
    /// 이동은 speed * dt + 오버슈트 방지 + 회전 속도 제한
    /// </summary>
    private void _MoveToward(Vector3 targetPosition, float dt)
    {
        var cur = GetPosition();
        var diff = targetPosition - cur;

        float distSq = diff.LengthSquared();
        if (distSq < 0.0001f) // 1cm^2
            return;

        float dist = MathF.Sqrt(distSq);
        var dir = diff / dist;

        float step = _tableData.monster_speed * dt;
        if (step > dist)
            step = dist; // 오버슈트 방지

        var newPos = cur + dir * step;

        // 목표 yaw (degree)
        float targetYawDeg = MathF.Atan2(dir.Z, dir.X) * (180f / MathF.PI);

        // 회전 속도 제한
        float maxDelta = MaxTurnDegPerSec * dt;
        _yawDeg = _MoveTowardsAngle(_yawDeg, targetYawDeg, maxDelta);

        _UpdateChangePositionAndRotation(newPos, _yawDeg);
    }

    private static float _MoveTowardsAngle(float current, float target, float maxDelta)
    {
        // -180~180 범위로 가장 짧은 회전 방향 선택
        float delta = ((target - current + 540f) % 360f) - 180f;

        if (MathF.Abs(delta) <= maxDelta)
            return target;

        return current + MathF.Sign(delta) * maxDelta;
    }

    public void UpdateAI(DateTime utcNow, Vector3 playerPosition)
    {
        // ---- dt 계산(틱이 흔들려도 속도 일정하게) ----
        float dt = (float)(utcNow - _lastMoveTime).TotalSeconds;
        _lastMoveTime = utcNow;

        // 이상치 방지 (서버가 멈췄다가 다시 돌면 한 번에 튀는 거 방지)
        if (dt <= 0f) dt = 0.1f;
        if (dt > 0.2f) dt = 0.2f;

        // ---- 상태 결정은 200ms마다만 ----
        if (utcNow >= _nextDecisionTime)
        {
            _DecideState(utcNow, playerPosition);
            _nextDecisionTime = utcNow.AddMilliseconds(DecisionMs);
        }

        // ---- 이동은 매틱마다 ----
        switch (_state)
        {
            case AIState.Chase:
            {
                // Attack -> Chase 전환 직후 살짝 멈춤(튐 제거)
                if (utcNow < _chaseMoveResumeTime)
                    return;

                _MoveToward(playerPosition, dt);
                return;
            }

            case AIState.Return:
            {
                _MoveToward(_startPosition, dt);

                // 앵커 도착 시 Idle 복귀
                var cur = GetPosition();
                var anchorDist = Vector3.Distance(_startPosition, cur);
                if (anchorDist < 1.0f)
                    _UpdateStateChange(utcNow, AIState.Idle);

                return;
            }

            case AIState.Attack:
            case AIState.Idle:
            case AIState.Sleep:
                _UpdateChangePositionAndRotation(GetPosition(), GetRotation());
                return;
            default:
                return;
        }
    }
    
    public override void UpdatePosition(Vector3 position, float rotation, int zoneId)
    {
        base.UpdatePosition(position, rotation, zoneId);
        
        _changePosition = _position;
        _changeRotation = _rotation;
    }

    private void _UpdateChangePositionAndRotation(Vector3 changePosition, float rotation)
    {
        _changePosition = changePosition;
        _changeRotation = rotation;
       
        _isChanged = true;
        
    }

    public override MonsterObjectBase ToPacket()
    {
        _packet.Id = _id;
        _packet.Position = GetPosition();
        _packet.ZoneId = GetZoneId();
        _packet.State = (int)_state;
        _packet.Rotation = GetRotation(); // degree로 통일된 값
        _packet.Type = GameObjectType.Monster;

        return _packet;
    }
}
