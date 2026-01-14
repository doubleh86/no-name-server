using SuperSocket.ProtoBase;

namespace WorldServer.Network;

// [bodySize][Key][Body]
public class NetworkPackage : IKeyedPackageInfo<int>
{
    public const int HeaderSize = 8;
    
    public int BodySize { get; set; }
    public int Key { get; set; }
    public byte[] Body { get; set; }

    public NetworkPackage(int key, int bodySize, byte[] body)
    {
        Body = body;
        Key = key;
        BodySize = bodySize;
    }

    public byte[] GetSendBuffer()
    {
        var sendBuffer = new byte[BodySize + HeaderSize];
        
        Buffer.BlockCopy(BitConverter.GetBytes(BodySize), 0, sendBuffer, 0, sizeof(int));
        Buffer.BlockCopy(BitConverter.GetBytes(Key), 0, sendBuffer, sizeof(int), sizeof(int));
        Buffer.BlockCopy(Body, 0, sendBuffer, HeaderSize, BodySize);

        return sendBuffer;
    }
}
