using Dapper;
using DbContext.Common.Models;

namespace DbContext.Common;

public static class CustomTableDataHelper
{
    public static SqlMapper.ICustomQueryParameter CreateCustomQueryParameter<TDbModel>(List<TDbModel> tableData) where TDbModel : IHasCustomTableData
    {
        var result = TDbModel.GetDataTable();
        foreach (var value in tableData)
        {
            var row = result.NewRow();
            value.SetCustomTableData(row);
            
            result.Rows.Add(row);
        }

        return result.AsTableValuedParameter(TDbModel.GetCustomTableName());
    }
}