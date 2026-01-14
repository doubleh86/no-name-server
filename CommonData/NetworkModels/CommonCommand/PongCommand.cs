using MemoryPack;
using NetworkProtocols.Socket;

namespace CommonData.NetworkModels.CommonCommand;

[MemoryPackable]
public partial class PongCommand : IBaseCommand
{
    public const int PongCommandId = 103;
    public long Identifier { get; set; }
    public long SendTimeMilliseconds;
}