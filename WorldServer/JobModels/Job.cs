using ServerFramework.CommonUtils.Helper;

namespace WorldServer.JobModels;

public abstract class Job(LoggerService loggerService)
{
    protected readonly LoggerService _loggerService = loggerService;
    public abstract ValueTask ExecuteAsync();
}