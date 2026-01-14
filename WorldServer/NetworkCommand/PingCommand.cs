
using CommonData.NetworkModels.CommonCommand;
using NetworkProtocols.Socket;
using SuperSocket.Command;
using SuperSocket.Server.Abstractions.Session;
using WorldServer.Network;

namespace WorldServer.NetworkCommand;

[Command(Key = CommonData.NetworkModels.CommonCommand.PingCommand.PingCommandId)]
public class PingCommand : IAsyncCommand<NetworkPackage>
{
    public async ValueTask ExecuteAsync(IAppSession session, NetworkPackage package, CancellationToken cancellationToken)
    {
        var receivedPackage = MemoryPackHelper.Deserialize<CommonData.NetworkModels.CommonCommand.PingCommand>(package.Body);
        var returnCommand = new PongCommand
                            {
                                SendTimeMilliseconds = receivedPackage.SendTimeMilliseconds
                            };

        var sendPackage = NetworkHelper.CreateSendPacket(PongCommand.PongCommandId, returnCommand);
        await session.SendAsync(sendPackage.GetSendBuffer(), cancellationToken);
    }
}