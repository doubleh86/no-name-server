using MemoryPack;
using NetworkProtocols.Socket;

namespace CommonData.NetworkModels.CommonCommand;

[MemoryPackable]
public partial class DisconnectedCommand : IBaseCommand
{
    public const int DisconnectedCommandId = 101;
    public long Identifier { get; set; }
    
    public string Message { get; set; } = string.Empty;
    public int Reason { get; set; }
}