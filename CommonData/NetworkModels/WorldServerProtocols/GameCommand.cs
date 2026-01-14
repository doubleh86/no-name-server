using MemoryPack;

namespace NetworkProtocols.Socket.WorldServerProtocols;

[MemoryPackable]
public partial class GameCommandRequest : IBaseCommand
{
    public long Identifier { get; set; }
    public int CommandId { get; set; }
    public byte[] CommandData { get; set; }
}

[MemoryPackable]
public partial class GameCommandResponse : IBaseCommand
{
    public long Identifier { get; set; }
    public int CommandId { get; set; }
    public byte[] CommandData { get; set; }
}

