using System.Data;

namespace DbContext.Common.Models;

public interface IHasCustomTableData
{
    static abstract string GetCustomTableName();
    void SetCustomTableData(DataRow row);
    static abstract DataTable GetDataTable();
}