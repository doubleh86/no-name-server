using System.Collections.Concurrent;
using System.Numerics;
using CommonData.CommonModels;
using DbContext.GameDbContext;
using DataTableLoader.Models;
using DataTableLoader.Helper;
using NetworkProtocols.Socket.WorldServerProtocols;
using NetworkProtocols.Socket.WorldServerProtocols.GameProtocols;
using ServerFramework.CommonUtils.Helper;
using ServerFramework.SqlServerServices.Models;
using SuperSocket.Server.Abstractions;
using WorldServer.GameObjects;
using WorldServer.JobModels;
using WorldServer.Network;
using WorldServer.Services;
using WorldServer.Utils;
using WorldServer.WorldHandler.WorldDataModels;
#pragma warning disable CS8632 // The annotation for nullable reference types should only be used in code within a '#nullable' annotations context.

namespace WorldServer.WorldHandler;

public partial class WorldInstance : IDisposable
{
    private readonly IGameDbContext _dbContext; // Read 용 dbContext 
    private readonly string _roomId;
    private readonly ConcurrentQueue<Job> _jobQueue = new();
    private bool _isDisposed;
    private int _exitOnce;
    
    // Worker Task
    private readonly CancellationTokenSource _jobCts = new();
    private readonly SemaphoreSlim _jobSignal = new(0, int.MaxValue);
    private Task? _jobWorkerTask;
    
    private readonly LoggerService _loggerService;
    private readonly GlobalDbService _globalDbService;
    
    private readonly Dictionary<GameCommandId, Func<byte[], ValueTask>> _commandHandlers = new();
    private PlayerObject _worldOwner;

    public string GetRoomId() => _roomId;
    private UserSessionInfo _GetUserSessionInfo() => _worldOwner.GetSessionInfo();
    private WorldMapInfo _worldMapInfo;
    
    
    public WorldInstance(string roomId, LoggerService loggerService, GlobalDbService dbService)
    {
        _roomId = roomId;
        _loggerService = loggerService;
        _globalDbService = dbService;

        _dbContext = GameDbContextWrapper.Create();
        _RegisterGameHandler();
        _StartJobWorker();
    }

    private void _StartJobWorker()
    {
        if (_jobWorkerTask != null)
            return;

        _jobWorkerTask = Task.Run(async () =>
        {
            await _ProcessJob(_jobCts.Token);
        });
    }

    private async ValueTask _ProcessJob(CancellationToken token)
    {
        try
        {
            while (token.IsCancellationRequested == false)
            {
                await _jobSignal.WaitAsync(token);
                while (token.IsCancellationRequested == false && _jobQueue.TryDequeue(out var job) == true)
                {
                    try
                    {
                        await job.ExecuteAsync();
                    }
                    catch (WorldServerException e)
                    {
                        _loggerService.Warning($"In Game Error [{e.Message}]", e);
                    }
                    catch (Exception e)
                    {
                        _loggerService.Error($"Job failed [{e.Message}]", e);
                    }
                }
            }
        }
        catch (OperationCanceledException)
        {
            _loggerService.Information("World instance maybe disposed");
        }
        catch (Exception e)
        {
            _loggerService.Error("World instance job worker error", e);
        }
    }
    
    public async ValueTask InitializeAsync(UserSessionInfo sessionInfo)
    {
        await _InitializeWorldAsync(sessionInfo);
    }

    private async ValueTask _InitializeWorldAsync(UserSessionInfo sessionInfo)
    {
        _worldOwner = new PlayerObject(IdGenerator.NextId(sessionInfo.Identifier), new Vector3(0, 0, 0), sessionInfo, 0);
        var playerInfo = await _dbContext.GetPlayerInfoAsync(1); // Test 용도
        if (playerInfo == null)
            throw new WorldServerException(WorldErrorCode.NotFoundPlayerInfo, $"Session Info {sessionInfo.Identifier}");    
        
        _worldOwner.SetPlayerInfo(playerInfo);
        await _RoadCurrentWorldMapAsync(playerInfo.last_world_id);
    }

    private ValueTask _RoadCurrentWorldMapAsync(int worldId)
    {
        var worldMapInfo = DataTableHelper.GetData<WorldInfo>(worldId);
        if (worldMapInfo == null)
            return ValueTask.CompletedTask;

        _worldMapInfo = new WorldMapInfo(_worldOwner.AccountId, _loggerService);
        _worldMapInfo.Initialize(worldId);

        _worldMapInfo.AddObject(_worldOwner);
        
        return ValueTask.CompletedTask;
    }

    public async ValueTask HandleGameCommand(GameCommandId command, byte[] commandData)
    {
        try
        {
            if (IsAliveWorld() == false)
                return;
            
            if (_commandHandlers.TryGetValue(command, out var handler) == false)
                return;
            
            await handler(commandData);
        }
        catch (Exception e)
        {
            _loggerService.Warning($"Command failed [{e.Message}][{command}]", e);
        }
    }

    private void _Push(Job action)
    {
        if (_isDisposed == true)
            return;
        
        _jobQueue.Enqueue(action);
        _jobSignal.Release();
    }
    
    public bool IsAliveWorld()
    {
        if (_isDisposed == true)
            return false;
        
        if (_worldMapInfo == null)
            return false;

        if (_worldOwner == null)
            return false;
        
        var session = _worldOwner.GetSessionInfo();
        if(session == null)
            return false;

        if (session.State == SessionState.Closed || session.State == SessionState.None)
            return false;
        
        return true;
    }

    private void _AutoSaveWorldState()
    {
        var owner = _worldOwner;
        if (owner == null)
            return;

        if (Interlocked.Exchange(ref _autoSaving, 1) == 1)
            return;

        _globalDbService.PushJob(owner.AccountId, async (dbContext) =>
        {
            try
            {
                if (owner.IsSaveDirty() == false)
                    return;
                
                var playerInfoResult = owner.GetPlayerInfoWithSave(true);
                await dbContext.AutoSaveInfoAsync(playerInfoResult);
                
                // TODO : world state update
                _Push(new GlobalDbResultJob<PlayerObject>(_loggerService, owner, (player) =>
                {
                    player.OnAutoSaveSuccess();
                    return ValueTask.CompletedTask;
                }));
            }
            catch (DatabaseException ex)
            {
                _loggerService.Warning($"AutoSaveInfoAsync failed for Account:{owner.AccountId}", ex);
            }
            catch (Exception e)
            {
                _loggerService.Warning($"AutoSaveInfoAsync failed for Account:{owner.AccountId}", e);
            }
            finally
            {
                Interlocked.Exchange(ref _autoSaving, 0);
            }
        });
    }

    private async ValueTask _SendGameCommandPacket(GameCommandId commandId, byte[] commandData)
    {
        if (_worldOwner == null)
            return;
        
        var packet = new GameCommandResponse
                     {
                         CommandId = (int)commandId,
                         CommandData = commandData
                     };

        var sendData = NetworkHelper.CreateSendPacket((int)WorldServerKeys.GameCommandResponse, packet);
        await _worldOwner.GetSessionInfo().SendAsync(sendData.GetSendBuffer());
    }

    public void ExitWorld(string reason)
    {
        if (Interlocked.CompareExchange(ref _exitOnce, 1, 0) == 1)
            return;
        
        _loggerService.Information($"World Exit Reason : {reason} | roomId = {_roomId}");
        Dispose();
    }

    private void _FinalAutoSaveWorldState()
    {
        var owner = _worldOwner;
        if (owner == null)
            return;

        try
        {
            using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(2));
            _globalDbService.PushJobAndWaitAsync(owner.AccountId, async dbContext =>
            {
                var info = owner.GetPlayerInfoWithSave(true);
                await dbContext.AutoSaveInfoAsync(info);
            }, cts.Token).GetAwaiter().GetResult();
        }
        catch (Exception e)
        {
            _loggerService.Warning($"Final autosave failed on dispose [{e.Message}]", e);
        }
    }
    public void Dispose()
    {
        if(_isDisposed) 
            return;

        _FinalAutoSaveWorldState();
        _isDisposed = true;
        
        _jobCts.Cancel();
        _jobSignal.Release();

        try
        {
            _jobWorkerTask?.Wait(TimeSpan.FromSeconds(2));
        }
        catch
        {
            _loggerService.Warning("Job worker task wait failed");
        }
        
        _jobQueue.Clear();
        _commandHandlers.Clear();
        
        _worldMapInfo?.Dispose();
        _worldMapInfo = null;
        _worldOwner = null;
        
        _dbContext?.Dispose();
        
        _jobSignal.Dispose();
        _jobCts.Dispose();
    }
}