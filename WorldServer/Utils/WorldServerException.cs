namespace WorldServer.Utils;

public class WorldServerException : Exception
{
    public override string Message { get; }
    public readonly WorldErrorCode ResultCode;

    public WorldServerException(WorldErrorCode resultCode, string message)
    {
        Message = message;
        ResultCode = resultCode;
    }    
}