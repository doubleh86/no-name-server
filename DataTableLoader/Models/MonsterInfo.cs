using System.ComponentModel.DataAnnotations.Schema;

namespace DataTableLoader.Models;

public class MonsterInfo : BaseData, IPrepareLoad, ICloneable
{
    public int monster_id { get; set; }
    public int monster_skill_1 { get; set; }
    public int monster_skill_2 { get; set; }
    public int monster_skill_3 { get; set; }
    
    public int monster_type { get; set; }
    public int monster_hp { get; set;}
    
    public float monster_speed { get; set; }
    public float max_anchor_distance { get; set; }
    
    [NotMapped] public List<int> monsterSkills { get; set; }
    protected override long GetKey()
    {
        return monster_id;
    }

    public void PrepareLoad()
    {
        monsterSkills = [monster_skill_1, monster_skill_2, monster_skill_3];
    }

    public object Clone()
    {
        var clone = (MonsterInfo)MemberwiseClone();
        if (monsterSkills != null)
        {
            clone.monsterSkills = new List<int>(monsterSkills);
        }
        
        return clone;
    }
}