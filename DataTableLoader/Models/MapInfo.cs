using System.Numerics;

namespace DataTableLoader.Models;

// size_x / chunk_size = max chunk x <= 이건 딱 떨어지게 설정
// size_z / chunk_size = max chunk z <= 이것도 딱 떨어지게 설정 
public class MapInfo : BaseData, ICloneable, IPrepareLoad
{
    public int zone_id { get; set; }
    public int world_id { get; set; }
    
    // 1000 이면 1km
    public int size_x { get; set; } 
    public int size_z { get; set; }
    public int chunk_size {get;set;} // 1당 1m
    
    public int world_offset_x { get; set; }
    public int world_offset_z { get; set; }
    
    
    public int MaxChunkX;
    public int MaxChunkZ;
    public Vector3 WorldOffset;
    
    protected override long GetKey()
    {
        return zone_id;
    }

    public object Clone()
    {
        return new MapInfo
        {
            zone_id = zone_id,
            world_id = world_id,
            size_x = size_x,
            size_z = size_z,
            world_offset_x = world_offset_x,
            world_offset_z = world_offset_z,
            chunk_size = chunk_size,
            MaxChunkX = MaxChunkX,
            MaxChunkZ = MaxChunkZ,
            WorldOffset = WorldOffset
        };
    }

    public void PrepareLoad()
    {
        MaxChunkX = (int)Math.Ceiling(size_x / (double)chunk_size);
        MaxChunkZ = (int)Math.Ceiling(size_z / (double)chunk_size);
        WorldOffset = new Vector3(world_offset_x, 0, world_offset_z);
    }
}