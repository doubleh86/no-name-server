using ClientTest.Socket;

namespace ClientTest.Helpers;

public static class TCPNetworkHelper
{
    public static NetworkPackage MakePackage(int key, byte[] body)
    {
        var package = new NetworkPackage
        {
            Key = key,
            BodySize = body.Length,
            Body = new byte[body.Length]
        };

        Array.Copy(body, package.Body, package.BodySize);
        return package;
    }
}