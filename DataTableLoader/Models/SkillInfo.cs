namespace DataTableLoader.Models;

public class SkillInfo : BaseData
{
    public int skill_id { get; set; }
    public int skill_cool_time { get; set;} // skill cool time in seconds
    public float skill_range { get; set; }
    public float skill_damage { get; set; }
    protected override long GetKey()
    {
        return skill_id;
    }
}