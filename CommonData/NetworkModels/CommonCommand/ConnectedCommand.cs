using MemoryPack;
using NetworkProtocols.Socket;

namespace CommonData.NetworkModels.CommonCommand;

[MemoryPackable]
public partial class ConnectedCommand : IBaseCommand
{
    public const int ConnectedCommandId = 100;
    public long Identifier { get; set; }
    
    public int Code { get; set; }
    public string Message { get; set; } = string.Empty;
    public bool IsSuccess { get; set; }
}
