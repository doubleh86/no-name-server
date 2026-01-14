using NetworkProtocols.Socket.WorldServerProtocols.GameProtocols;
using ServerFramework.CommonUtils.Helper;

namespace WorldServer.JobModels;

public class ActionJob<T> : Job where T : GameCommandBase
{
    private readonly T _data;
    private readonly Func<T, ValueTask> _action;

    public ActionJob(T data, Func<T, ValueTask> action, LoggerService loggerService = null) : base(loggerService)
    {
        _data = data;
        _action = action;
    }
    
    public override async ValueTask ExecuteAsync() => await _action(_data);
    
}