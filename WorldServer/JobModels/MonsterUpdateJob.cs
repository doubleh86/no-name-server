using System.Collections.Concurrent;
using System.Numerics;
using ServerFramework.CommonUtils.DateTimeHelper;
using ServerFramework.CommonUtils.Helper;
using WorldServer.GameObjects;
using WorldServer.WorldHandler.WorldDataModels;

namespace WorldServer.JobModels;


public class MonsterUpdateJob : Job
{
    private const int ParallelThreshold = 100; // ai가 복잡해지면 낮춘다
    private readonly Func<List<MonsterObject>, ValueTask> _action;
    private readonly List<MapCell> _nearByCells;
    
    private readonly Vector3 _playerPosition;

    public MonsterUpdateJob(Vector3 playerPosition, List<MapCell> nearByCells, 
        Func<List<MonsterObject>, ValueTask> action, 
        LoggerService loggerService) : base(loggerService)
    {
        _nearByCells = nearByCells;
        _action = action;
        _playerPosition = playerPosition;
    }
    
    public override async ValueTask ExecuteAsync()
    {
        var utcNow = TimeZoneHelper.UtcNow;

        var seen = new HashSet<long>(capacity: _nearByCells.Count * 64);
        var targetMonsters = new List<MonsterObject>(capacity: _nearByCells.Count * 64);
        
        foreach (var cell in _nearByCells)
        {
            foreach (var (_, obj) in cell.GetMapObjects())
            {
                if (obj is not MonsterObject monster)
                    continue;

                if (seen.Add(monster.GetId()) == false)
                    continue;
                
                targetMonsters.Add(monster);
            }
        }

        if (targetMonsters.Count < 1)
            return;
        
        try
        {
            if (targetMonsters.Count >= ParallelThreshold)
            {
                var errors = new ConcurrentQueue<Exception>();
                var parallelismCount = Math.Max(1, Environment.ProcessorCount - 1);
                var options = new ParallelOptions { MaxDegreeOfParallelism = parallelismCount };
                
                Parallel.ForEach(targetMonsters, options, monster =>
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
                foreach (var monster in targetMonsters)
                {
                    monster.UpdateAI(utcNow, _playerPosition);
                }    
            }
        }
        catch (Exception e)
        {
            _loggerService.Warning("Error in MonsterUpdateJob", e);
        }
        
        await _action(targetMonsters);
    }
}