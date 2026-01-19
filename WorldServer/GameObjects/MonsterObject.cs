using System.Numerics;
using CommonData.CommonModels.Enums;
using CommonData.NetworkModels.WorldServerProtocols.GameProtocols;
using DataTableLoader.Models;
using NetworkProtocols.Socket.WorldServerProtocols.Models;

namespace WorldServer.GameObjects;

public class MonsterObject : GameObject
{
    private readonly MonsterGroup _group;
    private readonly MonsterObjectBase _packet = new();
    private readonly MonsterInfo _tableData;

    private AIState _state;
    private DateTime _nextDecisionTime = DateTime.UtcNow;
    
    public AIState GetState() => _state;
    public MonsterGroup GetGroup() => _group;

    
    public MonsterObject(long id, Vector3 position, int zoneId, MonsterGroup group, MonsterInfo tableData)
        : base(id, zoneId, position, GameObjectType.Monster)
    {
        _group = group;
        _tableData = tableData.Clone() as MonsterInfo;
    }

    private void _UpdateStateChange(AIState newState)
    {
        if (_state == newState)
            return;
        
        _isChanged = true;
        _state = newState;
    }

    private void _UpdateState(Vector3 playerPosition)
    {
        var currentPosition = GetPosition();

        if (_state == AIState.Return)
        {
            var anchorDistance = Vector3.Distance(_group.AnchorPosition, currentPosition);
            if (anchorDistance < 1.0f)
                _UpdateStateChange(AIState.Idle);

            return;
        }

        // 앵커에서 너무 멀면 복귀
        var anchorDist = Vector3.Distance(_group.AnchorPosition, currentPosition);
        if (anchorDist > 45f)
        {
            _UpdateStateChange(AIState.Return);
            return;
        }

        var distance = Vector3.Distance(playerPosition, currentPosition);

        // 공격 중 멀어지면 쫓아가기
        if (_state == AIState.Attack)
        {
            if (distance > 3f)   // 공격 범위 벗어남
            {
                _UpdateStateChange(AIState.Chase);
                return;
            }
        }

        if (distance < 3f)
        {
            _UpdateStateChange(AIState.Attack);
            return;
        }

        if (distance < 25f)
        {
            _UpdateStateChange(AIState.Chase);
            return;
        }

        _UpdateStateChange(AIState.Idle);
    }

    private void _Move(Vector3 targetPosition)
    {
        var diff = targetPosition - GetPosition();
        if (diff.LengthSquared() < 0.001f)
            return;
                
        var direction = Vector3.Normalize(diff);
        var newPos = GetPosition() + direction * 0.5f;
        
        float newRotation = MathF.Atan2(direction.Z, direction.X);
        Console.WriteLine($"Move: {newPos} {newRotation}");
        _UpdateChangePositionAndRotation(newPos, newRotation);
    }

    public void UpdateAI(DateTime utcNow, Vector3 playerPosition)
    {
        if (utcNow < _nextDecisionTime)
            return;
        
        _UpdateState(playerPosition);
        _nextDecisionTime = utcNow.AddMilliseconds(500);
        switch (_state)
        {
            case AIState.Idle: // UserCheck
                break;
            case AIState.Sleep: // 계속 잔다?
                break;
            case AIState.Chase:
            {
                _Move(playerPosition);
                return;
            }

            case AIState.Return:
            {
                _Move(_group.AnchorPosition);
                return;
            }
                
        }
    }

    public override MonsterObjectBase ToPacket()
    {
        _packet.Id = _id;
        _packet.Position = GetPosition();
        _packet.ZoneId = GetZoneId();
        _packet.State = (int)_state;
        _packet.Rotation = GetRotation();
        _packet.Type = GameObjectType.Monster;

        return _packet;
    }

}