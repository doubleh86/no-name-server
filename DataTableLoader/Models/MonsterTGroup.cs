using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Numerics;

namespace DataTableLoader.Models;

public class MonsterTGroup : BaseData, IPrepareLoad, ICloneable
{
    public int monster_group_id { get; set; }
    
    [MaxLength(255)]
    public string monster_id_list { get; set; }
    public int world_id { get; set; }
    // public int zone_id { get; set; }
    public int position_x { get; set; }
    public int position_z { get; set; }
    public int position_y { get; set; }
    public float roam_radius { get; set; }

    [NotMapped] public List<long> MonsterList;
    [NotMapped] public Vector3 AnchorPosition;  
    
    protected override long GetKey()
    {
        return monster_group_id;
    }

    public void PrepareLoad()
    {
        MonsterList = monster_id_list.Split('˜').Select(long.Parse).ToList();
        AnchorPosition = new Vector3(position_x, position_y, position_z);
    }

    public object Clone()
    {
        var clone = (MonsterTGroup)MemberwiseClone();
        if (MonsterList != null)
        {
            clone.MonsterList = new List<long>(MonsterList);
        }
        
        return clone;
    }
}