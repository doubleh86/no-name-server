using CommonData.CommonModels;
using CommonData.CommonModels.Enums;
using DbContext.Common;
using MySqlConnector;
using ServerFramework.CommonUtils.RDBUtils;
using ServerFramework.MySqlServices.MySqlCommandModel;
using ServerFramework.MySqlServices.MySqlDapperUtils;

namespace DbContext.GameDbContext.MySqlContext.QueryCommand;

public class ItemUseCommandAsync (MySqlDapperServiceBase dbContext, MySqlTransaction transaction = null) 
    : QueryCommandBaseAsync<int, int>(dbContext, transaction)
{
    private const string _ItemUseQuery = "UPDATE inventory_info SET quantity = quantity - @itemCount " +
                                         "WHERE account_id = @accountId AND product_id = @itemId";
    
    public struct InParameters : IDbInParameters
    {
        public long accountId  { get; init; }
        public int itemId { get; init; }
        public int itemCount { get; init; }
    }
    
    public override async Task<int> ExecuteQueryAsync(IDbInParameters inParameters)
    {
        if (inParameters is not InParameters inParams)
            throw new DbContextException(DbErrorCode.InParameterWrongType, $"[{GetType().Name}] Parameter Type is wrong");

        var result = await _RunQueryReturnModelAsync(_ItemUseQuery,
            new { inParams.itemCount, inParams.accountId, inParams.itemId });
        
        return result.FirstOrDefault();
    }
}