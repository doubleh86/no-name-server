using DataTableLoader.Helper;
using DataTableLoader.Utils;
using DbContext.GameDbContext;
using Microsoft.Extensions.Options;
using ServerFramework.CommonUtils.DateTimeHelper;
using ServerFramework.CommonUtils.Helper;
using ServerFramework.SqlServerServices.Models;
using SuperSocket.Server;
using SuperSocket.Server.Abstractions;
using WorldServer.Network;
using WorldServer.WorldHandler.Utils;

namespace WorldServer.Services;

public class WorldServerService : SuperSocketService<NetworkPackage>
{
    private readonly UserService _userService;
    private readonly WorldService _worldService;
    private readonly LoggerService _loggerService;
    private readonly GlobalDbService _globalDbService;
    
    private readonly ConfigurationHelper _configurationHelper;
    private Dictionary<string, SqlServerDbInfo> _sqlServerDbInfoList = new();

    public UserService GetUserService() => _userService;
    public WorldService GetWorldService() => _worldService;
    public LoggerService GetLoggerService() => _loggerService;
    public ConfigurationHelper GetConfigHelper() => _configurationHelper;
    public GlobalDbService GetGlobalDbService() => _globalDbService;

    public WorldServerService(IServiceProvider serviceProvider, IOptions<ServerOptions> serverOptions) : base(serviceProvider, serverOptions)
    {
        _userService = serviceProvider.GetService(typeof(UserService)) as UserService;
        _globalDbService = serviceProvider.GetService(typeof(GlobalDbService)) as GlobalDbService;
        _worldService = serviceProvider.GetService(typeof(WorldService)) as WorldService;
        _loggerService = serviceProvider.GetService(typeof(LoggerService)) as LoggerService;
        _configurationHelper = serviceProvider.GetService(typeof(ConfigurationHelper)) as ConfigurationHelper;
    }
    
    private void _InitializeSqlServerDbInfo()
    {
        var sqlSettings = _configurationHelper.GetSection<SqlServerDbSettings>(nameof(SqlServerDbSettings));
        _sqlServerDbInfoList = sqlSettings.ConnectionInfos;
        
        foreach (var (key, value) in sqlSettings.ConnectionInfos)
        {
            switch (key)
            {
                case "GameDbContext":
                    GameDbContextWrapper.SetDefaultServerInfo(value);
                    break;
            }
        }
    }
    
    protected override async ValueTask OnStartedAsync()
    {
        try
        {
            var configFiles = new List<string> { "appsettings.json", "Settings/redisSettings.json", "Settings/sqlSettings.json"};
            _configurationHelper.Initialize(configFiles);
        
            _InitializeSqlServerDbInfo();
            _loggerService.CreateLogger(_configurationHelper.Configuration);
        
            var shardCount = _configurationHelper.GetValue("GlobalDbShardCount", 4);
            _globalDbService.Initialize(shardCount, _loggerService);

            _InitializeDataTable();
        
            var serviceTimeZone = _configurationHelper.GetValue("ServiceTimeZone", "UTC");
            TimeZoneHelper.Initialize(serviceTimeZone);
        
            var minWorker = _configurationHelper.GetValue("MinWorkerThreads", 120);
            var minIOThread = _configurationHelper.GetValue("MinIOThreads", 120);
        
            ThreadPool.SetMinThreads(Math.Max(minWorker, Environment.ProcessorCount * 2), minIOThread);
        
            _worldService.Initialize(this);
            _worldService.StartGlobalTicker();
            
            // WorldDefinition All Preload
            WorldDefinitionCache.LoadAll();
        
            await base.OnStartedAsync();
        }
        catch (Exception e)
        {
            Console.WriteLine($"Maybe error {e.Message}");
        }
        
    }

    private void _InitializeDataTable()
    {
        if (_sqlServerDbInfoList.TryGetValue(nameof(DataTableDbService), out var sqlInfo) == false)
        {
            _loggerService.Error("Can't find DataTableDbService connection info");
            return;
        }
        
        DataTableHelper.Initialize(sqlInfo, _loggerService);
        DataTableHelper.ReloadTableData();
    }

    protected override async ValueTask OnStopAsync()
    {
        _worldService?.Dispose();
        await Task.Delay(100);
        _globalDbService?.Dispose();
        _userService?.Dispose();
        
        await base.OnStopAsync();
    }
    
}