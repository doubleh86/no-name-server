using System.Numerics;
using ClientTest.Models;
using ClientTest.Socket;
using ClientTest.Socket.TCPClient;
using CommonData.CommonModels;
using NetworkProtocols.Socket;
using NetworkProtocols.Socket.WorldServerProtocols;
using NetworkProtocols.Socket.WorldServerProtocols.GameProtocols;

namespace ClientTest.Handlers;

public partial class WorldServerHandler(ITCPClient client) : TCPPacketHandler(client)
{
    protected readonly Dictionary<int, Action<byte[]>> _gameHandler = new();

    private readonly CancellationTokenSource _cts = new();
    private bool _isTicking = false; 
    
    protected override void _RegisterHandler()
    {
        base._RegisterHandler();
        
        _handler.Add((int)WorldServerKeys.ResponseWorldJoin, OnWorldJoin);
        _handler.Add((int)WorldServerKeys.GameCommandResponse, OnGameCommand);
        
        _gameHandler.Add((int)GameCommandId.MonsterUpdateCommand, _OnMonsterUpdateCommand);
        _gameHandler.Add((int)GameCommandId.UseItemCommand, _OnItemUseCommand);
        _gameHandler.Add((int)GameCommandId.SpawnGameObject, _OnSpawnGameObject);
    }

    private void OnGameCommand(NetworkPackage obj)
    {
        var receivedPackage = MemoryPackHelper.Deserialize<GameCommandResponse>(obj.Body);
        if(receivedPackage == null)
            return;

        if (_gameHandler.TryGetValue(receivedPackage.CommandId, out var handler) == false)
            return;
        
        handler(receivedPackage.CommandData);
    }

    private void OnWorldJoin(NetworkPackage obj)
    {
        var receivedPackage = MemoryPackHelper.Deserialize<WorldJoinCommandResponse>(obj.Body);
        if(receivedPackage == null)
            return;
        
        Console.WriteLine($"Received World Join Response: RoomId={receivedPackage.RoomId}");
        _StartTick();
    }

    private void _StartTick()
    {
        if (_isTicking)
            return;
        
        _isTicking = true;
        Task.Run(async () =>
        {
            if (_client is not TestSession client)
                return;
            
            while (_cts.IsCancellationRequested == false)
            {
                var randomX = new Random().Next(0, 1800);
                var randomZ = new Random().Next(0, 1200); 
                _SendMoveCommand(client, randomX, randomZ, 2.0f);
                
                
                var startTime = DateTime.UtcNow;
                var elapsed = DateTime.UtcNow - startTime;
                var delay = Math.Max(0, 100 - (int)elapsed.TotalMilliseconds);
                await Task.Delay(delay, _cts.Token);
                
            }
        });
    }

    private void _SendMoveCommand(TestSession client, int x, int z, float rotation)
    {
        client.SendGameCommand(new GameCommandRequest
        {
            CommandId = (int)GameCommandId.MoveCommand,
            CommandData = MemoryPackHelper.Serialize(new MoveCommand()
            {
                Position = new Vector3(x, 0, z),
                Rotation = rotation
            })
        });
    }

    private void _SendItemUseCommand(TestSession client, int itemId)
    {
        client.SendGameCommand(new GameCommandRequest()
        {
            CommandId = (int)GameCommandId.UseItemCommand,
            CommandData = MemoryPackHelper.Serialize(new UseItemCommand()
            {
                ItemId = itemId,
                UseCount = 1
            }) 
        });
    }
    
    protected override void OnConnected(NetworkPackage package)
    {
        base.OnConnected(package);

        if (_client is not TestSession client)
            return;
        
        client.SendWorldJoinCommand();
        Console.WriteLine("World Join Command Sent");
    }
}