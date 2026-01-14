using System.ComponentModel.DataAnnotations.Schema;
using System.Numerics;

namespace MySqlDataTableLoader.Models;

public class MonsterTGroup : BaseData, IPrepareLoad, ICloneable
{
    public int monster_group_id { get; set; }
    public string monster_id_list { get; set; }
    public int world_id { get; set; }
    // public int zone_id { get; set; }
    public int position_x { get; set; }
    public int position_z { get; set; }
    public int position_y { get; set; }
    
    [NotMapped] public List<int> MonsterList;
    [NotMapped] public Vector3 AnchorPosition;  
    
    protected override int GetKey()
    {
        return monster_group_id;
    }

    public void PrepareLoad()
    {
        MonsterList = monster_id_list.Split('˜').Select(int.Parse).ToList();
        AnchorPosition = new Vector3(position_x, position_y, position_z);
    }

    public object Clone()
    {
        var clone = (MonsterTGroup)MemberwiseClone();
        if (MonsterList != null)
        {
            clone.MonsterList = new List<int>(MonsterList);
        }
        
        return clone;
    }
}