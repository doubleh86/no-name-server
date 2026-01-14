using System.Numerics;
using MySqlDataTableLoader.Models;
using MySqlDataTableLoader.Utils.Helper;
using ServerFramework.CommonUtils.Helper;
using WorldServer.GameObjects;
using WorldServer.WorldHandler.Utils;

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
public class WorldMapInfo : IDisposable
{
    private readonly LoggerService _loggerService;
    
    private readonly long _accountId;
    private int _worldMapId;
    
    private readonly Dictionary<int, MapCell[,]> _zoneCells = new();
    private readonly Dictionary<int, MapInfo> _zoneInfos = new();
    
    private readonly List<MonsterGroup> _monsterGroups = new();
    private readonly Dictionary<long, int[]> _worldZoneGrid = new();
    
    public int GetWorldMapId() => _worldMapId;
    public WorldMapInfo(long accountId, LoggerService loggerService)
    {
        _accountId = accountId;
        _loggerService = loggerService;
    }
    
    public void Initialize(int worldMapId)
    {
        if(_zoneCells.Count > 0 || _monsterGroups.Count > 0 || _worldMapId != 0)
            ClearWorld();
        
        _worldMapId = worldMapId;
        var mapCache = WorldDefinitionCache.Get(_worldMapId);
       
        _ApplyDefinition(mapCache);
        
        _InitializeCells(mapCache.Zones.ToList());
        _InitializeMonsters();
    }

    private void _ApplyDefinition(WorldDefinition definition)
    {
        foreach (var (zoneId, info) in definition.ZoneInfos)
            _zoneInfos[zoneId] = info;
        
        foreach(var (gridKey, zones) in definition.WorldZoneGrid)
            _worldZoneGrid[gridKey] = zones;

    }
    

    private void  _AddZone(MapInfo mapInfo)
    {
        if (mapInfo.world_id != _worldMapId)
            return;

        var cells = new MapCell[mapInfo.MaxChunkX, mapInfo.MaxChunkZ];
        for (int x = 0; x < mapInfo.MaxChunkX; x++)
        {
            for (int z = 0; z < mapInfo.MaxChunkZ; z++)
            {
                cells[x, z] = new MapCell(mapInfo.zone_id, x, z, mapInfo.WorldOffset, mapInfo.chunk_size);    
            }
        }

        _zoneCells[mapInfo.zone_id] = cells;
    }
    
    
    private void _InitializeMonsters()
    {
        var monsterGroupList = MySqlDataTableHelper
            .GetDataList<MonsterTGroup>()
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
                var tableData = MySqlDataTableHelper.GetData<MonsterInfo>(monsterId);
                if (tableData == null)
                    continue;
                
                var cell = _GetCell(position);
                if (cell == null)
                    break;
                
                var spawnedMonster = new MonsterObject(IdGenerator.NextId(_accountId), position, zoneId, registerMonsterGroup, tableData);
                cell.Enter(spawnedMonster);
                registerMonsterGroup.AddMember(spawnedMonster);
            }
            
            if(registerMonsterGroup.MonsterCount < 1)
                continue;
            
            _monsterGroups.Add(registerMonsterGroup);
        }
    }

    private void _InitializeCells(List<MapInfo> mapInfos)
    {
        foreach (var info in mapInfos)
        {
            _AddZone(info.Clone() as MapInfo);
        }
    }

    public MapCell AddObject(GameObject obj)
    {
        var cell = GetCell(obj.GetPosition());
        cell?.Enter(obj);

        return cell;
    }

    public MapCell GetCell(Vector3 worldPosition, int zoneId = 0)
    {
        if (zoneId != 0 && _zoneInfos.TryGetValue(zoneId, out var mapInfo) == true)
        {
            var minX = mapInfo.world_offset_x;
            var maxX = mapInfo.world_offset_x + mapInfo.chunk_size * mapInfo.MaxChunkX;
            var minZ = mapInfo.world_offset_z;
            var maxZ = mapInfo.world_offset_z + mapInfo.chunk_size * mapInfo.MaxChunkZ;

            if (worldPosition.X >= minX && worldPosition.X < maxX &&
                worldPosition.Z >= minZ && worldPosition.Z < maxZ)
            {
                var (xIdx, zIdx) = _GetCellIndex(worldPosition, mapInfo);
                return _GetCellByIndex(zoneId, xIdx, zIdx);
            }
        }

        return _GetCell(worldPosition);
    }

    private MapCell _GetCell(Vector3 worldPosition)
    {
        var findZoneId = _FindZoneByWorldPosition(worldPosition);
        if(_zoneInfos.TryGetValue(findZoneId, out var mapInfo) == false)
            return null;
        
        var (xIdx, zIdx) = _GetCellIndex(worldPosition, mapInfo);
        return _GetCellByIndex(findZoneId, xIdx, zIdx);
    }

    private (int, int) _GetCellIndex(Vector3 worldPosition, MapInfo mapInfo)
    {
        int xIdx = (int)MathF.Floor((worldPosition.X - mapInfo.world_offset_x) / mapInfo.chunk_size);
        int zIdx = (int)MathF.Floor((worldPosition.Z - mapInfo.world_offset_z) / mapInfo.chunk_size);
        
        return (xIdx, zIdx);
    }

    private int _FindZoneByWorldPosition(Vector3 worldPosition)
    {
        var gx = (long)Math.Floor(worldPosition.X / WorldDefinition.ZoneLookupGridSizeMeters);
        var gz = (long)Math.Floor(worldPosition.Z / WorldDefinition.ZoneLookupGridSizeMeters);
        var gridKey = WorldDefinition.GetGridKey(gx, gz);

        if (_worldZoneGrid.TryGetValue(gridKey, out var candidateZones) == false)
            return -1;
        
        foreach(var zoneId in candidateZones)
        {
            if(_zoneInfos.TryGetValue(zoneId, out var zoneEntry) == false)
                continue;
            
            var minX = zoneEntry.world_offset_x;
            var maxX = zoneEntry.world_offset_x + zoneEntry.chunk_size * zoneEntry.MaxChunkX;
            var minZ = zoneEntry.world_offset_z;
            var maxZ = zoneEntry.world_offset_z + zoneEntry.chunk_size * zoneEntry.MaxChunkZ;

            if (worldPosition.X >= minX && worldPosition.X < maxX &&
                worldPosition.Z >= minZ && worldPosition.Z < maxZ)
            {
                return zoneId;
            }
        }
        
        return -1;
    }

    public List<MapCell> GetWorldNearByCells(int zoneId, Vector3 worldPosition, int range = 1)
    {
        var centerCell = GetCell(worldPosition, zoneId);
        if (centerCell == null)
            return [];
        
        var baseZoneId = centerCell.ZoneId;
        var nearCells = new List<MapCell>();
        for (var x = centerCell.X - range; x <= centerCell.X + range; x++)
        {
            for (var z = centerCell.Z - range; z <= centerCell.Z + range; z++)
            {
                var cell = _GetCellByIndex(baseZoneId, x, z);
                if (cell != null)
                {
                    nearCells.Add(cell);
                    continue;
                }
                
                var neighborCell = _GetNeighborZoneCell(baseZoneId, x, z);
                if (neighborCell == null)
                    continue;
                
                nearCells.Add(neighborCell);
            }
        }
        
        return nearCells;
    }

    private MapCell _GetNeighborZoneCell(int zoneId, int xIdx, int zIdx)
    {
        if (_zoneInfos.TryGetValue(zoneId, out var mapInfo) == false)
            return null;
        
        float worldX = mapInfo.world_offset_x + (xIdx * mapInfo.chunk_size) + (mapInfo.chunk_size * 0.5f);
        float worldZ = mapInfo.world_offset_z + (zIdx * mapInfo.chunk_size) + (mapInfo.chunk_size * 0.5f);
        
        var targetWorldPosition = new Vector3(worldX, 0, worldZ);
        int targetZoneId = _FindZoneByWorldPosition(targetWorldPosition);
        if (targetZoneId == -1 || targetZoneId == zoneId)
            return null;
        
        return GetCell(targetWorldPosition, targetZoneId);
    }

    private MapCell _GetCellByIndex(int zoneId, int x, int z)
    {
        if (_zoneCells.TryGetValue(zoneId, out var cells) == false)
            return null;
        
        
        int maxIdxX = cells.GetLength(0);
        int maxIdxZ = cells.GetLength(1);

        if (x < 0 || x >= maxIdxX || z < 0 || z >= maxIdxZ)
            return null;
        
        return cells[x, z];
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
    public void ClearWorld()
    {
        foreach (var zone in _zoneCells.Values)
        {
            foreach (var cell in zone)
            {
                cell.Dispose();
            }
        }
        
        _monsterGroups.Clear();
        _zoneInfos.Clear();
        _zoneCells.Clear();
        _worldZoneGrid.Clear();

        _worldMapId = 0;
    }

    public void Dispose()
    {
        ClearWorld();
    }
}