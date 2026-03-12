using CommonData.CommonModels;
using NetworkProtocols.Socket;
using NetworkProtocols.Socket.WorldServerProtocols;
using NetworkProtocols.Socket.WorldServerProtocols.GameProtocols;
using SuperSocket.Command;
using SuperSocket.Server.Abstractions.Session;
using WorldServer.Network;
using WorldServer.Services;

namespace WorldServer.NetworkCommand;

[Command(Key = WorldServerKeys.GameCommandRequest)]
public class GameCommand(WorldService worldService) : IAsyncCommand<NetworkPackage>
{
    private readonly WorldService _worldService = worldService;

    public ValueTask ExecuteAsync(IAppSession session, NetworkPackage package, CancellationToken cancellationToken)
    {
        if (session is not UserSessionInfo userSessionInfo)
            throw new ArgumentException("Invalid session type");
        
        var worldInstance = userSessionInfo.GetWorldInstance();
        if(worldInstance == null)
            throw new Exception("Not joined world");
        
        var request = MemoryPackHelper.Deserialize<GameCommandRequest>(package.Body);
        if (_worldService.EnqueueGameCommand(worldInstance, (GameCommandId)request.CommandId, request.CommandData) == false)
            throw new Exception("World shard enqueue failed");

        return ValueTask.CompletedTask;
    }
}
