using MemoryPack;
using NetworkProtocols.Socket;

namespace CommonData.NetworkModels.CommonCommand;

[MemoryPackable]
public partial class PingCommand : IBaseCommand
{
    public const int PingCommandId = 102;
    public long Identifier { get; set; }
    public long SendTimeMilliseconds;
    
}


