using System.Numerics;
using CommonData.NetworkModels.WorldServerProtocols.GameProtocols;
using DataTableLoader.Models;
using DataTableLoader.Helper;
using NetworkProtocols.Socket;
using NetworkProtocols.Socket.WorldServerProtocols.GameProtocols;
using WorldServer.GameObjects;
using WorldServer.Utils;
using WorldServer.WorldHandler.WorldDataModels;

namespace WorldServer.WorldHandler;

public partial class WorldInstance
{
    private const int _MaxMonsterUpdateCount = 50;

    private async ValueTask _OnMoveCommand(MoveCommand command)
    {
        var oldZoneId = _worldOwner.GetZoneId();
        var oldPosition = _worldOwner.GetPosition();
        var oldCell = _worldMapInfo.GetCell(oldPosition, oldZoneId);
        var newCell = _worldMapInfo.GetCell(command.Position);
        if (newCell == null)
            return;
            
        _worldOwner.UpdatePosition(command.Position, command.Rotation, newCell.ZoneId);
        if (oldCell == newCell)
        {
            return;
        }
            
        oldCell?.Leave(_worldOwner.GetId());
        newCell.Enter(_worldOwner);

        var (enterCell, leaveCell) = _worldMapInfo.UpdatePlayerView(oldCell?.ZoneId ?? -1, 
                                                                    newCell.ZoneId, 
                                                                    oldPosition, command.Position);
            
        await _SendViewUpdateBatched(isSpawn: true, cells:enterCell);
        await _SendViewUpdateBatched(isSpawn: false, cells:leaveCell);
    }
    
    private async ValueTask _SendViewUpdateBatched(bool isSpawn, List<MapCell> cells)
    {
        if (cells == null || cells.Count == 0)
            return;
        
        var seen = new HashSet<long>(capacity: cells.Count * 16);
        var batch = new List<GameObjectBase>(capacity: _MaxViewUpdateBatchCount);

        foreach (var cell in cells)
        {
            var objects = cell.GetMapObjects().Values;
            if (objects.Count == 0)
                continue;

            foreach (var obj in objects)
            {
                var id = obj.GetId();

                if (_worldOwner != null && id == _worldOwner.GetId())
                    continue;
                
                if (seen.Add(id) == false)
                    continue;
                
                batch.Add(obj.ToPacket());
                if (batch.Count >= _MaxViewUpdateBatchCount)
                {
                    var packet = new UpdateGameObjects {IsSpawn = isSpawn, GameObjects = batch};
                    var commandData = MemoryPackHelper.Serialize<UpdateGameObjects>(packet);
                    
                    await _SendGameCommandPacket(GameCommandId.SpawnGameObject, commandData);
                    batch.Clear();
                }
            }
        }

        if (batch.Count > 0)
        {
            var packet = new UpdateGameObjects {IsSpawn = isSpawn, GameObjects = batch};
            var remainData = MemoryPackHelper.Serialize<UpdateGameObjects>(packet);
            await _SendGameCommandPacket(GameCommandId.SpawnGameObject, remainData);
        }

    }


    private async ValueTask _OnChangeWorldCommand(ChangeWorldCommand command)
    {
        if (Interlocked.Exchange(ref _isChangingWorld, 1) == 1)
            return;

        try
        {
            if(DataTableHelper.GetData<WorldInfo>(command.WorldId) == null)
                throw new WorldServerException(WorldErrorCode.WrongPacket, $"World not found {command.WorldId}");
                
            _worldMapInfo.ClearWorld();
            _worldMapInfo.Initialize(command.WorldId);

            var spawnPosition = new Vector3(40, 0, 40);
            var spawnCell = _worldMapInfo.GetCell(spawnPosition);
            if (spawnCell == null)
            {
                throw new WorldServerException(WorldErrorCode.WrongPacket, $"Spawn cell not found for position {spawnPosition}");
            }

            _worldOwner.UpdatePosition(spawnPosition, 0, spawnCell.ZoneId);
            spawnCell.Enter(_worldOwner);

            var response = new ChangeWorldCommandResponse()
                           {
                               WorldId = _worldMapInfo.GetWorldMapId(),
                               Player = _worldOwner.ToPacket(),
                           };

            await _SendGameCommandPacket(GameCommandId.ChangeWorldResponse, MemoryPackHelper.Serialize(response));
        }
        catch (Exception e)
        {
            _loggerService.Error(e.Message, e);
        }
        finally
        {
            Interlocked.Exchange(ref _isChangingWorld, 0);
        }
    }
    private async ValueTask _OnMonsterUpdate(List<MonsterObject> monsters)
    {
        var updateMonsterGroups = new HashSet<MonsterGroup>();
        var dirtyMonsters = monsters.Where(x => x.IsChanged()).ToList();
        if (dirtyMonsters.Count == 0)
            return;
        
        foreach (var monster in dirtyMonsters)
        {
            var cell = _worldMapInfo.GetCell(monster.GetPosition(), monster.GetZoneId());
            var changeCell = _worldMapInfo.GetCell(monster.GetChangePosition());
            if (changeCell == null)
            {
                monster.ResetChanged();
                continue;
            }
                
            monster.UpdatePosition(monster.GetChangePosition(), monster.GetChangeRotation(), changeCell.ZoneId);
            monster.ResetChanged();
            
            updateMonsterGroups.Add(monster.GetGroup());
            
            if (cell == changeCell) 
                continue;
            
            changeCell.Enter(monster);
            cell.Leave(monster.GetId());
            
        }

        foreach (var monsterGroup in updateMonsterGroups)
        {
            monsterGroup.UpdateIsAnyMemberInCombat(_worldOwner);
        }
        
        for (var i = 0; i < dirtyMonsters.Count; i += _MaxMonsterUpdateCount)
        {
            int count = Math.Min(_MaxMonsterUpdateCount, dirtyMonsters.Count - i);
            var batchMonsters = dirtyMonsters.GetRange(i, count);

            var gameCommand = new MonsterUpdateCommand
            {
                Monsters = batchMonsters.Select(x => x.ToPacket()).ToList()
            };
           
            await _SendGameCommandPacket(GameCommandId.MonsterUpdateCommand, MemoryPackHelper.Serialize(gameCommand));
        }
        
    }
}