using System.Numerics;
using CommonData.CommonModels.Enums;
using MemoryPack;
using NetworkProtocols.Socket.WorldServerProtocols.GameProtocols;

namespace CommonData.NetworkModels.WorldServerProtocols.GameProtocols;


[MemoryPackable]
public partial class GameObjectBase
{
    public long Id { get; set; }
    public int ZoneId { get; set; }
    public float Rotation { get; set; }
    public Vector3 Position { get; set; }
    public GameObjectType Type { get; set; }
}

[MemoryPackable]
public partial class MonsterObjectBase : GameObjectBase
{
    public int State { get; set; }
}

[MemoryPackable]
public partial class MonsterUpdateCommand : GameCommandBase
{
    public List<MonsterObjectBase> Monsters { get; set; }
}

[MemoryPackable]
public partial class UpdateGameObjects : GameCommandBase
{
    public bool IsSpawn { get; set; }
    public List<GameObjectBase> GameObjects { get; set; }
}