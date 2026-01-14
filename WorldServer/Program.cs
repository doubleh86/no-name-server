// Start Console Program

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Serilog;
using ServerFramework.CommonUtils.Helper;
using ServerFramework.RedisService;
using SuperSocket.Command;
using SuperSocket.Server;
using SuperSocket.Server.Host;
using WorldServer.Network;
using WorldServer.NetworkCommand;
using WorldServer.Services;

var host = CreateHostBuilder().Build();

try
{
    Console.WriteLine($"Is Server GC: {System.Runtime.GCSettings.IsServerGC}");
    Console.WriteLine($"GC Latency Mode: {System.Runtime.GCSettings.LatencyMode}");
    await host.RunAsync();
}
catch (Exception e)
{
    Log.Error(e.ToString());
}
finally
{
    Log.CloseAndFlush();
}

return;

IHostBuilder CreateHostBuilder()
{
    
    var builder = SuperSocketHostBuilder.Create<NetworkPackage, PacketPipeLineFilter>();
    
    builder.UseSession<UserSessionInfo>();
    builder.UseHostedService<WorldServerService>();
    builder.UseCommand(InitializeCommand);
    
    builder.ConfigureServices((ctx, services) =>
    {
        services.AddSingleton<GlobalDbService>();
        services.AddSingleton<LoggerService>();
        services.AddSingleton<UserService>();
        services.AddSingleton<WorldService>();
        services.AddSingleton<RedisServiceManager>();
        services.AddSingleton<ConfigurationHelper>();
    });
    
    builder.ConfigureLogging((ctx, logging) =>
    {
        logging.AddConsole();
    });
    
    return builder;
}

void InitializeCommand(CommandOptions options)
{
    options.AddCommand<PingCommand>();
    options.AddCommand<WorldJoinCommand>();
    options.AddCommand<GameCommand>();
}