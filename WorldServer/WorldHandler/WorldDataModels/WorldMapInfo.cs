using System.Numerics;
using DataTableLoader.Models;
using DataTableLoader.Helper;
using ServerFramework.CommonUtils.Helper;
using WorldServer.GameObjects;

namespace WorldServer.WorldHandler.WorldDataModels;

/// <summary>
/// chunk_size 1 => 1m
/// size_x 1000 이면 1km
/// MaxChunkSize = (size_x / chunk_size) (2000 / 25 => 80)
/// 예시 ( size_x 2000, chunk_size 25 => MaxChunkSize 80)
/// Cell Size ( 25, 25 )
/// Zone Size ( 80, 80 )
/// Cell total count : 80 * 80 = 6400
/// 
/// </summary>
public class WorldMapInfo : MapInfoBase
{
    private readonly long _accountId;
    private readonly List<MonsterGroup> _monsterGroups = new();
    
    public int GetWorldMapId() => _worldMapId;
    public List<MonsterGroup> GetMonsterGroups() => _monsterGroups;
    public WorldMapInfo(long accountId, LoggerService loggerService) : base(loggerService)
    {
        _accountId = accountId;
    }
    
    public override void Initialize(int worldMapId)
    {
        if(_zoneCells.Count > 0 || _monsterGroups.Count > 0 || _worldMapId != 0)
            ClearWorld();
        
        base.Initialize(worldMapId);
        _InitializeMonsters();
    }

    private void _InitializeMonsters()
    {
        var monsterGroupList = DataTableHelper.GetDataList<MonsterTGroup>()
                                              .Where(x => x.world_id == _worldMapId);

        foreach (var monsterGroup in monsterGroupList)
        {
            var zoneId = _FindZoneByWorldPosition(monsterGroup.AnchorPosition);
            if (zoneId == -1)
            {
                _loggerService.Warning($"Can't find zone for monster {monsterGroup.monster_group_id}");
                continue;
            }
            
            var registerMonsterGroup = new MonsterGroup(monsterGroup.Clone() as MonsterTGroup, zoneId);
            var position = new Vector3(monsterGroup.position_x, monsterGroup.position_y, monsterGroup.position_z);
            
            registerMonsterGroup.Id = IdGenerator.NextId(_accountId);
            registerMonsterGroup.RoamRadius = 20;
            
            foreach (var monsterId in monsterGroup.MonsterList)
            {
                var tableData = DataTableHelper.GetData<MonsterInfo>(monsterId);
                if (tableData == null)
                    continue;
                
                var cell = GetCell(position);
                if (cell == null)
                    break;
                
                var spawnedMonster = new MonsterObject(IdGenerator.NextId(_accountId), position, zoneId, registerMonsterGroup, tableData);
                cell.Enter(spawnedMonster);
                registerMonsterGroup.AddMember(spawnedMonster);
                
                Console.WriteLine($"Spawned Monster {monsterId}|{cell.ZoneId}|{position.X},{position.Y},{position.Z}");
            }
            
            if(registerMonsterGroup.MonsterCount < 1)
                continue;
            
            _monsterGroups.Add(registerMonsterGroup);
        }
    }

    // Player AOI
    public (List<MapCell>, List<MapCell>) UpdatePlayerView(int oldZoneId, int newZoneId, Vector3 oldPosition, Vector3 newPosition)
    {
        if (oldZoneId == -1 || newZoneId == -1)
            return ([], []);
        
        var oldNearByCells = GetWorldNearByCells(oldZoneId, oldPosition, range: 2);
        var nearByCells = GetWorldNearByCells(newZoneId, newPosition, range: 2);

        var oldSet = oldNearByCells.ToHashSet();
        var newSet = nearByCells.ToHashSet();
        
        var enter = new List<MapCell>();
        foreach (var cell in newSet)
        {
            if(oldSet.Contains(cell) == false)
                enter.Add(cell);
        }
        
        var leave = new List<MapCell>();
        foreach (var cell in oldSet)
        {
            if(newSet.Contains(cell) == false)
                leave.Add(cell);
        }
        
        return (enter, leave);
    }

    // world 이동 시
    public override void ClearWorld()
    {
        base.ClearWorld();
        _monsterGroups.Clear();
    }

}