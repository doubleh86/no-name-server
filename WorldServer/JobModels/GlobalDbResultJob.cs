using ServerFramework.CommonUtils.Helper;

namespace WorldServer.JobModels;

public class GlobalDbResultJob<T> : Job where T : class
{
    private readonly Func<T, ValueTask> _action;
    private readonly T _inParameters; // Class Object 로 묶에서 한다.
    public GlobalDbResultJob(LoggerService loggerService, T inParameters, Func<T, ValueTask> action) : base(loggerService)
    {
        _action = action;
        _inParameters = inParameters;
    }

    public override async ValueTask ExecuteAsync() => await _action(_inParameters);
}