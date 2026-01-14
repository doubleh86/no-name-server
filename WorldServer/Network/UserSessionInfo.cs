using SuperSocket.Connection;
using SuperSocket.Server;
using WorldServer.Services;
using WorldInstance = WorldServer.WorldHandler.WorldInstance;

namespace WorldServer.Network;

public class UserSessionInfo : AppSession
{
    private long _identifier;
    public long Identifier => _identifier;
    private WorldServerService _serverService => Server as WorldServerService;
    private WorldInstance _worldInstance;

    public WorldInstance GetWorldInstance() => _worldInstance;
    public void SetWorldInstance(WorldInstance worldInstance)
    {
        _worldInstance = worldInstance;
    }
    
    public void RegisterIdentifier(long identifier)
    {
        _identifier = identifier;
        _serverService.GetUserService().AddUser(this);
    }

    public async ValueTask SendAsync(byte[] sendData, CancellationToken cancellationToken = default)
    {
        await Connection.SendAsync(sendData, cancellationToken);
    }
    
    protected override ValueTask OnSessionConnectedAsync()
    {
        _serverService.GetLoggerService().Information("Connected to WorldServer");
        // _serverService.GetUserService().AddUser(this);
        
        return ValueTask.CompletedTask;
    }
    
    protected override ValueTask OnSessionClosedAsync(CloseEventArgs e)
    {
        Console.WriteLine($"Closed from server [{e.Reason}]");
        _serverService.GetUserService().RemoveUser(_identifier);
        if (_worldInstance != null)
        {
            _serverService.GetWorldService().RemoveWorldInstance(_worldInstance.GetRoomId());
        }
           
        return ValueTask.CompletedTask;
    }
}