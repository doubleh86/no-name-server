using CommonData.CommonModels;
using CommonData.CommonModels.Enums;
using DbContext.Common;
using DbContext.GameDbContext.DbResultModel;
using MySqlConnector;
using ServerFramework.CommonUtils.RDBUtils;
using ServerFramework.MySqlServices.MySqlCommandModel;
using ServerFramework.MySqlServices.MySqlDapperUtils;

namespace DbContext.GameDbContext.MySqlContext.QueryCommand;

public class GetPlayerInfoCommandAsync (MySqlDapperServiceBase dbContext, MySqlTransaction transaction = null) 
    : QueryCommandBaseAsync<PlayerInfoResult, PlayerInfoResult>(dbContext, transaction)
{
    private const string _GetPlayerInfoQuery = "SELECT account_id, player_level, player_exp, " +
                                               "last_world_id, last_zone_id," +
                                               "position_x, position_y, position_z FROM " +
                                               "player_info WHERE account_id = @accountId";
    public struct InParameters : IDbInParameters
    {
        public long accountId  { get; init; }
    }
    
    public override async Task<PlayerInfoResult> ExecuteQueryAsync(IDbInParameters inParameters)
    {
        if (inParameters is not InParameters inParams)
            throw new DbContextException(DbErrorCode.InParameterWrongType, $"[{GetType().Name}] Parameter Type is wrong");

        var result =  await _RunQueryReturnModelAsync(_GetPlayerInfoQuery, 
            new { inParams.accountId });
        
        return result.FirstOrDefault();
    }
}