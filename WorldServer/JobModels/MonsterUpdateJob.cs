using System.Collections.Concurrent;
using System.Numerics;
using ServerFramework.CommonUtils.DateTimeHelper;
using ServerFramework.CommonUtils.Helper;
using WorldServer.GameObjects;
using WorldServer.ResourcePool;
using WorldServer.WorldHandler.WorldDataModels;

namespace WorldServer.JobModels;


public class MonsterUpdateJob(LoggerService loggerService) : Job(loggerService)
{
    private const int ParallelThreshold = 100; // ai가 복잡해지면 낮춘다
    
    private Func<List<MonsterObject>, ValueTask> _action;
    private List<MapCell> _nearByCells;
    private Vector3 _playerPosition;
    
    private readonly HashSet<long> _seen = new(capacity: 256);
    private readonly List<MonsterObject> _targetMonsters = new(capacity: 256);
    

    public void Initialize(Vector3 playerPosition, List<MapCell> nearByCells,
        Func<List<MonsterObject>, ValueTask> action)
    {
        _nearByCells = nearByCells;
        _action = action;
        _playerPosition = playerPosition;
    }

    public void Reset()
    {
        _nearByCells = null;
        _action = null;
    }

    private void _UpdateMonsterAI(DateTime utcNow)
    {
        try
        {
            if (_targetMonsters.Count >= ParallelThreshold)
            {
                var errors = new ConcurrentQueue<Exception>();
                var parallelismCount = Math.Max(1, Environment.ProcessorCount - 1);
                var options = new ParallelOptions { MaxDegreeOfParallelism = parallelismCount };
                
                Parallel.ForEach(_targetMonsters, options, monster =>
                {
                    try
                    {
                        monster.UpdateAI(utcNow, _playerPosition);
                    }
                    catch (Exception e)
                    {
                        errors.Enqueue(e);
                    }
                });

                if (errors.IsEmpty == false)
                {
                    _loggerService.Warning($"Errors occurred during monster AI updates : {errors.Count}");
                }
            }
            else
            {
                foreach (var monster in _targetMonsters)
                {
                    monster.UpdateAI(utcNow, _playerPosition);
                }    
            }
            
            ResolveMonsterSeparation(_targetMonsters, 1.5f);
        }
        catch (Exception e)
        {
            _loggerService.Warning("Error in MonsterUpdateJob", e);
        }

    }
    
    private void ResolveMonsterSeparation(List<MonsterObject> monsters, float minDist)
    {
        float minDistSq = minDist * minDist;

        for (int i = 0; i < monsters.Count; i++)
        {
            for (int j = i + 1; j < monsters.Count; j++)
            {
                var a = monsters[i];
                var b = monsters[j];

                var pa = a.GetChangePosition();
                var pb = b.GetChangePosition();

                float dx = pb.X - pa.X;
                float dz = pb.Z - pa.Z;

                float distSq = dx * dx + dz * dz;

                if (distSq < 0.000001f)
                {
                    dx = 0.001f;
                    dz = 0.0f;
                    distSq = dx * dx + dz * dz;
                }

                if (distSq >= minDistSq)
                    continue;

                float dist = MathF.Sqrt(distSq);

                // 서로 반씩 밀기
                float push = (minDist - dist) * 0.5f;

                // 한 틱에 너무 많이 밀리는 거 방지 (옵션)
                push = MathF.Min(push, 0.2f);

                float nx = dx / dist;
                float nz = dz / dist;

                // 위치 보정
                pa.X -= nx * push;
                pa.Z -= nz * push;

                pb.X += nx * push;
                pb.Z += nz * push;

                // 회전: 서로 벌어지는 방향을 바라보게
                // a는 (-nx, -nz) 방향
                float yawA = MathF.Atan2(-nz, -nx) * (180f / MathF.PI);
                // b는 (nx, nz) 방향
                float yawB = MathF.Atan2(nz, nx) * (180f / MathF.PI);

                if (dist < minDist * 0.5f)
                {
                    a.ForceSetPositionAndRotation(pa, yawA);
                    b.ForceSetPositionAndRotation(pb, yawB);
                }
                else
                {
                    a.ForceSetPosition(pa);
                    b.ForceSetPosition(pb);
                }
            }
        }
    }

    private void _CollectTargetMonsters()
    {
        foreach (var cell in _nearByCells)
        {
            foreach (var (_, obj) in cell.GetMapObjects())
            {
                if (obj is not MonsterObject monster)
                    continue;

                if (_seen.Add(monster.GetId()) == false)
                    continue;
                
                _targetMonsters.Add(monster);
            }
        }
    }
    public override async ValueTask ExecuteAsync()
    {
        try
        {
            _seen.Clear();
            _targetMonsters.Clear();
            
            var utcNow = TimeZoneHelper.UtcNow;
            
            _CollectTargetMonsters();
            if (_targetMonsters.Count < 1)
                return;
        
            _UpdateMonsterAI(utcNow);    
            await _action(_targetMonsters);
        }
        finally
        {
            MonsterUpdateJobPool.Return(this);
        }
    }
}