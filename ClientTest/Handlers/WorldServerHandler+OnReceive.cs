using CommonData.CommonModels.Enums;
using CommonData.NetworkModels.WorldServerProtocols.GameProtocols;
using NetworkProtocols.Socket;
using NetworkProtocols.Socket.WorldServerProtocols.GameProtocols;

namespace ClientTest.Handlers;

public partial class WorldServerHandler
{
    private void _OnMonsterUpdateCommand(byte[] data)
    {
        var packet = MemoryPackHelper.Deserialize<MonsterUpdateCommand>(data);
        
        // Console.WriteLine($"Monster Update : {packet.Monsters.Count}");
    }

    private void _OnItemUseCommand(byte[] data)
    {
        var packet = MemoryPackHelper.Deserialize<UseItemCommand>(data);
        Console.WriteLine($"Item Use {packet.ItemId}");
    }

    private void _OnSpawnGameObject(byte[] data)
    {
        var packet = MemoryPackHelper.Deserialize<UpdateGameObjects>(data);
        if (packet == null)
            return;

        if (packet.IsSpawn == false)
            return;

        if (packet.GameObjects == null)
            return;
        var list = packet.GameObjects.FindAll(x => x.Type == GameObjectType.Player);
        // foreach (var player in list)
        // {
        //     Console.WriteLine($"Spawn Player {player.Id}|{player.ZoneId}|{player.Position}");
        // }
        
        //Console.WriteLine($"Spawn GameObject {packet.GameObjects.Count} | Spawn Type : {packet.IsSpawn}");
    }
}