using System.Collections.Concurrent;
using System.Numerics;
using ServerFramework.CommonUtils.Helper;
using WorldServer.GameObjects;
using WorldServer.JobModels;
using WorldServer.WorldHandler.WorldDataModels;

namespace WorldServer.ResourcePool;

public static class MonsterUpdateJobPool
{
    private static LoggerService _loggerService;
    private static readonly ConcurrentStack<MonsterUpdateJob> _jobPool = new();

    public static void Initialize(int initialCapacity, LoggerService loggerService)
    {
        _loggerService = loggerService;
        for (int i = 0; i < initialCapacity; i++)
        {
            _jobPool.Push(new MonsterUpdateJob(_loggerService));
        }
    }

    public static MonsterUpdateJob Rent(Vector3 pos, List<MapCell> cells, Func<List<MonsterObject>, ValueTask> action)
    {
        if (_jobPool.TryPop(out var job) == false)
        {
            job = new MonsterUpdateJob(_loggerService);
        }
        
        job.Initialize(pos, cells, action);
        return job;
    }
    
    public static void Return(MonsterUpdateJob job)
    {
        job.Reset();
        _jobPool.Push(job);
    }
}