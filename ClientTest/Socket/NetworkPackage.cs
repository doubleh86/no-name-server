using ClientTest.Socket.TCPClient;

namespace ClientTest.Socket;

// [bodySize][Key][Body]
public class NetworkPackage
{
    public const int HeaderSize = 8;

    public int BodySize { get; set; }
    public int Key { get; set; }
    public byte[] Body { get; set; }

    public void SetData(byte[] data, int startIndex, int size)
    {
        if (size >= TCPCommon.MaxReceivePacketSize)
            return;

        if (size <= 0)
        {
            Console.WriteLine("size <= 0");
            return;
        }

        Body = new byte[size];
        Array.Copy(data, startIndex, Body, 0, size);
    }
}