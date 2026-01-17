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
        }
        catch (Exception e)
        {
            _loggerService.Warning("Error in MonsterUpdateJob", e);
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