using MemoryPack;

namespace NetworkProtocols.Socket;

public static class MemoryPackHelper
{
    public static T Deserialize<T>(byte[] bytes) where T : class, new()
    {
        var protocol = new T();
        
        MemoryPackSerializer.Deserialize(bytes, ref protocol);
        return protocol;
    }

    public static byte[] Serialize<T>(T protocol) where T : class
    { 
        return MemoryPackSerializer.Serialize(protocol);
    }
}