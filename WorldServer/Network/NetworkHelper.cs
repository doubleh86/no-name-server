using NetworkProtocols.Socket;

namespace WorldServer.Network;

public static class NetworkHelper
{
    public static NetworkPackage CreateSendPacket<T>(int key, T data) where T : class
    {
        var bodyData = MemoryPackHelper.Serialize(data);
        var package = new NetworkPackage(key, bodyData.Length, bodyData);

        return package;
    }
}