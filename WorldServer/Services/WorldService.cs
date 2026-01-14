using System.Collections.Concurrent;
using WorldServer.Network;
using WorldInstance = WorldServer.WorldHandler.WorldInstance;

namespace WorldServer.Services;

public class WorldService : IDisposable
{
    private WorldServerService _serverService;
    private List<WorldInstance>[] _shardWorldLists;
    private object[] _shardLocks;
    private readonly ConcurrentDictionary<string, int> _roomShardMap = new();
    
    private readonly ConcurrentDictionary<string, WorldInstance> _worldInstances = new();
    private readonly int _workerCount = Environment.ProcessorCount;
    private readonly CancellationTokenSource _cts = new();

    public void Initialize(WorldServerService serverService)
    {
        _shardWorldLists = new List<WorldInstance>[_workerCount];
        _shardLocks = new object[_workerCount];
        for (int i = 0; i < _workerCount; i++)
        {
            _shardWorldLists[i] = new List<WorldInstance>();
            _shardLocks[i] = new object();
        }
        
        _SetServerService(serverService);    
    }
    private void _SetServerService(WorldServerService serverService)
    {
        _serverService = serverService;
    }
    public async Task<WorldInstance> CreateWorldInstance(string roomId, UserSessionInfo userSessionInfo)
    {
        var newWorldInstance = new WorldInstance(roomId, _serverService.GetLoggerService(), 
                                                 _serverService.GetGlobalDbService());
        
        await newWorldInstance.InitializeAsync(userSessionInfo);
        
        if (_worldInstances.TryAdd(roomId, newWorldInstance) == false)
            return null;

        int bestShardIndex = 0;
        int minCount = int.MaxValue;

        for (int i = 0; i < _workerCount; i++)
        {
            int count = _shardWorldLists[i].Count;
            if (count < minCount)
            {
                minCount = count;
                bestShardIndex = i;
            }
        }
        
        lock (_shardLocks[bestShardIndex])
        {
            _shardWorldLists[bestShardIndex].Add(newWorldInstance);
            _roomShardMap.TryAdd(roomId, bestShardIndex);
        }
        
        return newWorldInstance;
    }

    public WorldInstance GetWorldInstance(string roomId)
    {
        return _worldInstances.TryGetValue(roomId, out var instance) ? instance : null;
    }

    public void RemoveWorldInstance(string roomId)
    {
        if (_worldInstances.TryRemove(roomId, out var worldInstance) == false)
            return;

        _roomShardMap.TryRemove(roomId, out var shardIndex);

        if (shardIndex >= 0 && shardIndex < _shardWorldLists.Length)
        {
            lock (_shardLocks[shardIndex])
            {
                _shardWorldLists[shardIndex].Remove(worldInstance);
            }
        }
        
        worldInstance.ExitWorld("Disconnect");
    }

    public void StartGlobalTicker()
    {
        for(int i = 0; i < _workerCount; i++)
        {
            int shardIndex = i;
            Task.Run(() => _GlobalTickLoop(shardIndex));
        }
    }

    private async Task _GlobalTickLoop(int shardIndex)
    {
        var timer = new PeriodicTimer(TimeSpan.FromMilliseconds(30));
        var myShardList = _shardWorldLists[shardIndex];
        List<WorldInstance> tickSnapShot = new();
        List<WorldInstance> removeList = new();
        
        while (await timer.WaitForNextTickAsync(_cts.Token))
        {
            try
            {
                lock (_shardLocks[shardIndex])
                {
                    tickSnapShot.AddRange(myShardList);
                }
                
                foreach (var worldInstance in tickSnapShot)
                {
                    if (worldInstance.IsAliveWorld() == false)
                    {
                        removeList.Add(worldInstance);
                        continue;
                    }
                        
                    worldInstance.Tick();
                }

                if (removeList.Count > 0)
                {
                    _RemoveDeadWorlds(shardIndex, myShardList, removeList);
                    removeList.Clear();
                }
                
                tickSnapShot.Clear();
            }
            catch (OperationCanceledException)
            {
                break;
            }
            catch (Exception e)
            {
                _serverService.GetLoggerService().Warning($"GlobalTickLoop Error : {e.Message}", e);
            }
        }
    }

    private void _RemoveDeadWorlds(int shardIndex, List<WorldInstance> myShardList, List<WorldInstance> removeList)
    {
        lock (_shardLocks[shardIndex])
        {
            foreach (var worldInstance in removeList)
            {
                myShardList.Remove(worldInstance);
                _worldInstances.TryRemove(worldInstance.GetRoomId(), out _);
                _roomShardMap.TryRemove(worldInstance.GetRoomId(), out _);
                        
                worldInstance.Dispose();
            }    
        }
    }

    public void Dispose()
    {
        _cts?.Cancel();
        _cts?.Dispose();
    }
}
    