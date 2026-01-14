using System.Collections.Concurrent;
using MySqlDataTableLoader.Models;
using MySqlDataTableLoader.Utils.Helper;
using WorldServer.Utils;

namespace WorldServer.WorldHandler.Utils;

public sealed class WorldDefinition
{
    // 월드를 구성하는 격자 크기 (m), Zone 후보를 빠르게 찾기 위해 사용
    public const float ZoneLookupGridSizeMeters = 100; // 100m 격자 구성

    public int WorldId { get; }
    public IReadOnlyDictionary<int, MapInfo> ZoneInfos { get; }
    public IReadOnlyDictionary<long, int[]> WorldZoneGrid { get; }
    public IReadOnlyList<MapInfo> Zones { get; }
    public static long GetGridKey(long gridX, long gridZ) => (gridX << 32) | (gridZ & 0xFFFFFFFFL);
    public WorldDefinition(int worldId)
    {
        WorldId = worldId;
        var world = MySqlDataTableHelper.GetData<WorldInfo>(worldId);
        if(world == null)
            throw new WorldServerException(WorldErrorCode.NotFoundZone, $"Not found world {worldId}");
        
        var zones = MySqlDataTableHelper.GetDataList<MapInfo>()
                                        .Where(x => x.world_id == worldId)
                                        .Select(x => x.Clone() as MapInfo)
                                        .Where(x => x != null)
                                        .ToList();
        
        Zones = zones;
        var zoneInfos = new Dictionary<int, MapInfo>(capacity: zones.Count);
        foreach (var z in zones)
        {
            if (z != null)
                zoneInfos[z.zone_id] = z;
        }
        
        ZoneInfos = zoneInfos;
        WorldZoneGrid = BuildWorldZoneGrid(zoneInfos);
    }
    
    
    private static IReadOnlyDictionary<long, int[]> BuildWorldZoneGrid(Dictionary<int, MapInfo> zoneInfos)
    {
        var temp = new Dictionary<long, List<int>>();

        foreach (var (zoneId, info) in zoneInfos)
        {
            float minX = info.world_offset_x;
            float maxX = minX + (info.chunk_size * info.MaxChunkX);
            float minZ = info.world_offset_z;
            float maxZ = minZ + (info.chunk_size * info.MaxChunkZ);

            int startX = (int)Math.Floor(minX / ZoneLookupGridSizeMeters);
            int endX = (int)Math.Ceiling(maxX / ZoneLookupGridSizeMeters) - 1;
            int startZ = (int)Math.Floor(minZ / ZoneLookupGridSizeMeters);
            int endZ = (int)Math.Ceiling(maxZ / ZoneLookupGridSizeMeters) - 1;

            endX = Math.Max(endX, startX);
            endZ = Math.Max(endZ, startZ);

            for (int gx = startX; gx <= endX; gx++)
            {
                for (int gz = startZ; gz <= endZ; gz++)
                {
                    var key = GetGridKey(gx, gz);
                    if (temp.TryGetValue(key, out var zones) == false)
                    {
                        zones = new List<int>(capacity: 4);
                        temp[key] = zones;
                    }

                    zones.Add(zoneId);
                }
            }
        }

        var frozen = new Dictionary<long, int[]>(capacity: temp.Count);
        foreach(var (key, list) in temp)
            frozen[key] = list.ToArray();

        return frozen;
    }
}

public static class WorldDefinitionCache
{
    private static readonly ConcurrentDictionary<int, WorldDefinition> _cache = new();
    public static WorldDefinition Get(int worldId) => _cache.GetOrAdd(worldId, x => new WorldDefinition(x));
}