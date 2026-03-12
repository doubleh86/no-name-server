using System.Collections.Concurrent;
using NetworkProtocols.Socket.WorldServerProtocols.GameProtocols;
using WorldServer.Network;
using WorldInstance = WorldServer.WorldHandler.WorldInstance;

namespace WorldServer.Services;

public class WorldService : IDisposable
{
    private WorldServerService _serverService;
    private WorldShardExecutor _worldShardExecutor;
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
        _worldShardExecutor = new WorldShardExecutor(_workerCount, _serverService.GetLoggerService());
    }

    private void _SetServerService(WorldServerService serverService)
    {
        _serverService = serverService;
    }

    private int _GetShardIndex(string roomId)
    {
        return (int)((uint)roomId.GetHashCode() % (uint)_workerCount);
    }

    public async Task<WorldInstance> CreateWorldInstance(string roomId, UserSessionInfo userSessionInfo)
    {
        var newWorldInstance = new WorldInstance(roomId, _serverService.GetLoggerService(), 
                                                 _serverService.GetGlobalDbService());
        
        await newWorldInstance.InitializeAsync(userSessionInfo);
        
        if (_worldInstances.TryAdd(roomId, newWorldInstance) == false)
            return null;

        int bestShardIndex = _GetShardIndex(roomId);
        
        lock (_shardLocks[bestShardIndex])
        {
            _shardWorldLists[bestShardIndex].Add(newWorldInstance);
            _roomShardMap.TryAdd(roomId, bestShardIndex);
        }

        newWorldInstance.BindShardDispatcher(job =>
        {
            if (_worldShardExecutor.TryEnqueue(bestShardIndex, () => newWorldInstance.ExecuteJobAsync(job)) == false)
                _serverService.GetLoggerService().Warning($"World shard enqueue failed: room={roomId}, shard={bestShardIndex}");
        });
        
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

        var hasShard = _roomShardMap.TryRemove(roomId, out var shardIndex);

        if (hasShard && shardIndex >= 0 && shardIndex < _shardWorldLists.Length)
        {
            lock (_shardLocks[shardIndex])
            {
                _shardWorldLists[shardIndex].Remove(worldInstance);
            }
        }

        if (hasShard == false)
        {
            worldInstance.ExitWorld("Disconnect");
            return;
        }

        if (_worldShardExecutor.TryEnqueue(shardIndex, () =>
            {
                worldInstance.ExitWorld("Disconnect");
                return ValueTask.CompletedTask;
            }) == false)
        {
            worldInstance.ExitWorld("Disconnect");
        }
    }

    public bool EnqueueGameCommand(WorldInstance worldInstance, GameCommandId commandId, byte[] commandData)
    {
        if (worldInstance == null)
            return false;

        if (_roomShardMap.TryGetValue(worldInstance.GetRoomId(), out var shardIndex) == false)
            return false;

        return _worldShardExecutor.TryEnqueue(shardIndex, () => worldInstance.HandleGameCommand(commandId, commandData));
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

                    if (_worldShardExecutor.TryEnqueue(shardIndex, () =>
                        {
                            worldInstance.Tick();
                            return ValueTask.CompletedTask;
                        }) == false)
                    {
                        _serverService.GetLoggerService().Warning($"World tick enqueue failed: room={worldInstance.GetRoomId()}, shard={shardIndex}");
                    }
                }

                if (removeList.Count > 0)
                {
                    lock (_shardLocks[shardIndex])
                    {
                        _RemoveDeadWorldsInternal(myShardList, removeList);
                    }
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

    // 여기서는 lock을 걸지 않는다. 
    // _shardLocks[...] 걸면 재진입 락이 걸림.
    private void _RemoveDeadWorldsInternal(List<WorldInstance> myShardList, List<WorldInstance> removeList)
    {
        foreach (var worldInstance in removeList)
        {
            var roomId = worldInstance.GetRoomId();
            myShardList.Remove(worldInstance);
            _worldInstances.TryRemove(roomId, out _);
            var hasShard = _roomShardMap.TryRemove(roomId, out var shardIndex);

            if (hasShard == false)
            {
                worldInstance.ExitWorld("DeadWorld");
                continue;
            }

            if (_worldShardExecutor.TryEnqueue(shardIndex, () =>
                {
                    worldInstance.ExitWorld("DeadWorld");
                    return ValueTask.CompletedTask;
                }) == false)
            {
                worldInstance.ExitWorld("DeadWorld");
            }
        }    
    }

    public void Dispose()
    {
        _cts?.Cancel();
        _worldShardExecutor?.Dispose();
        _cts?.Dispose();
    }
}
    
