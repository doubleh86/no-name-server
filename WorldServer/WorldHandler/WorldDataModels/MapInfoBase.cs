using System.Numerics;
using DataTableLoader.Models;
using ServerFramework.CommonUtils.Helper;
using WorldServer.GameObjects;
using WorldServer.WorldHandler.Utils;

namespace WorldServer.WorldHandler.WorldDataModels;

public abstract class MapInfoBase : IDisposable
{
    protected readonly LoggerService _loggerService;
    protected int _worldMapId;
    protected readonly Dictionary<int, MapCell[,]> _zoneCells = new();
    
    private readonly Dictionary<int, MapInfo> _zoneInfos = new();
    private readonly Dictionary<long, int[]> _worldZoneGrid = new();

    protected MapInfoBase(LoggerService loggerService)
    {
        _loggerService = loggerService;
    }

    public virtual void Initialize(int worldMapId)
    {
        _worldMapId = worldMapId;
        
        var mapCache = WorldDefinitionCache.Get(_worldMapId);
        _ApplyDefinition(mapCache);
        _InitializeCells(mapCache.Zones.ToList());
    }
    
    protected virtual void _ApplyDefinition(WorldDefinition definition)
    {
        foreach (var (zoneId, info) in definition.ZoneInfos)
            _zoneInfos[zoneId] = info;
        
        foreach(var (gridKey, zones) in definition.WorldZoneGrid)
            _worldZoneGrid[gridKey] = zones;
    }
    
    private void _InitializeCells(List<MapInfo> mapInfos)
    {
        foreach (var info in mapInfos)
        {
            _CreateCellAndAddZone(info.Clone() as MapInfo);
        }
    }
    
    private void  _CreateCellAndAddZone(MapInfo mapInfo)
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

    protected int _FindZoneByWorldPosition(Vector3 worldPosition)
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
    
    public MapCell AddObject(GameObject obj)
    {
        var cell = GetCell(obj.GetPosition());
        cell?.Enter(obj);

        return cell;
    }

    public virtual void ClearWorld()
    {
        foreach (var zone in _zoneCells.Values)
        {
            foreach (var cell in zone)
            {
                cell.Dispose();
            }
        }
        
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