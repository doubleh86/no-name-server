namespace DbContext.GameDbContext.DbResultModel;

public class PlayerInfoResult
{
    public long account_id { get; set; }
    public int player_level { get; set; }
    public int player_exp { get; set; }
    public int last_world_id { get; set; }
    public int last_zone_id { get; set; }
    public float position_x { get; set; }
    public float position_y { get; set; }
    public float position_z { get; set; }
}