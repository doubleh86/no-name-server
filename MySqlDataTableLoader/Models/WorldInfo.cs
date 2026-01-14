using System.ComponentModel.DataAnnotations.Schema;

namespace MySqlDataTableLoader.Models;

[Table("world_info")]
public class WorldInfo : BaseData
{
    public int world_id { get; set; }
    public string world_name { get; set; }
    
    protected override int GetKey()
    {
        return world_id;
    }
}