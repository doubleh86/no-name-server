using System.Collections.Concurrent;
using Microsoft.EntityFrameworkCore;
using MySqlDataTableLoader.Models;
using ServerFramework.CommonUtils.Helper;
using ServerFramework.SqlServerServices;
using ServerFramework.SqlServerServices.Models;

namespace MySqlDataTableLoader.Utils;

public class DataTableDbService : SqlServerServiceBase
{
    private readonly LoggerService _loggerService;
    private readonly ConcurrentDictionary<Type, object> _tableMapping = new();
    
    private DbSet<WorldInfo> world_info { get; set; }
    private DbSet<MapInfo> map_info { get; set; }
    private DbSet<MonsterTGroup> monster_group { get; set; }
    private DbSet<MonsterInfo> monster_info { get; set; }
    
    public DataTableDbService(SqlServerDbInfo settings, LoggerService loggerService, bool isLazyLoading = false) : base(settings)
    {
        UseLazyLoading(isLazyLoading);
        _loggerService = loggerService;
        
        _RegisterTableDbSet();
    }
    
    private void _RegisterTableDbSet()
    {
        _tableMapping.TryAdd(typeof(WorldInfo), world_info);
        _tableMapping.TryAdd(typeof(MapInfo), map_info);
        _tableMapping.TryAdd(typeof(MonsterTGroup), monster_group);
        _tableMapping.TryAdd(typeof(MonsterInfo), monster_info);
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<WorldInfo>(entity => entity.HasKey(data => data.world_id));
        modelBuilder.Entity<MapInfo>(entity => entity.HasKey(data => new {data.zone_id, data.world_id}));
        modelBuilder.Entity<MonsterTGroup>(entity => entity.HasKey(data => data.monster_group_id));
        modelBuilder.Entity<MonsterInfo>(entity => entity.HasKey(data => data.monster_id));
    }
    
    public List<T> LoadData<T>() where T : BaseData
    {
        try
        {
            if (_tableMapping.TryGetValue(typeof(T), out var data) == false)
            {
                _loggerService?.Warning("Need Add _tableMapping Dictionary (Check _RegisterTableDbSet() Method)");
                return null; 
            }
            
            return data is not DbSet<T> dbSet ? null : dbSet.ToList();
        }
        catch (Exception e)
        {
            _loggerService?.Warning($"Exception Load Data [Name {typeof(T).Name}]", e);
            return null;
        }
    }
    
}