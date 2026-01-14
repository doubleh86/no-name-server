using ServerFramework.SqlServerServices.DapperUtils;
using ServerFramework.SqlServerServices.Models;

namespace DbContext.Common.Models;

public interface IUseSlaveDbContext<T> where T : DapperServiceBase
{
    T MasterDbInfo { get; set; }
    T SlaveDbInfo { get; set; }
    void InitializedDbContexts(SqlServerDbInfo masterDbInfo, SqlServerDbInfo slaveDbInfo);
    T GetDbContext(bool isSlave);
}