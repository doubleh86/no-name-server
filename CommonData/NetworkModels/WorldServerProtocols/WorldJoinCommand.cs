using MemoryPack;

namespace NetworkProtocols.Socket.WorldServerProtocols;

[MemoryPackable]
public partial class WorldJoinCommandRequest : IBaseCommand
{
    public long Identifier { get; set; }
}

[MemoryPackable]
public partial class WorldJoinCommandResponse : IBaseCommand
{
    public string RoomId { get; set; }
    public long Identifier { get; set; }
}