using MemoryPack;

namespace NetworkProtocols.Socket.WorldServerProtocols.GameProtocols;

public enum GameCommandId
{
    MoveCommand = 1,
    MonsterUpdateCommand = 2,
    UseItemCommand = 3,
    ChangeWorldCommand = 4,
    SpawnGameObject,
    
    // Response
    UseItemResponse = 1001,
    ChangeWorldResponse = 1002,
}

// 필요 없을 듯
[MemoryPackable]
public partial class GameCommandBase
{
    public int CommandId { get; set; } // 사용 안하지만 memoryPack 에러 때문에 넣어둠.
}