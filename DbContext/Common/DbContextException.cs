using CommonData.CommonModels;
using CommonData.CommonModels.Enums;

namespace DbContext.Common;

public class DbContextException : Exception
{
    public override string Message { get; }
    public readonly DbErrorCode ResultCode;
    public DbContextException(DbErrorCode resultCode, string message)
    {
        Message = message;
        ResultCode = resultCode;
    }
}