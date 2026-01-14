using CommonData.CommonModels;
using CommonData.CommonModels.Enums;
using DbContext.Common;
using MySqlConnector;
using ServerFramework.CommonUtils.RDBUtils;
using ServerFramework.MySqlServices.MySqlCommandModel;
using ServerFramework.MySqlServices.MySqlDapperUtils;

namespace DbContext.GameDbContext.MySqlContext.QueryCommand;

public class UpdatePlayerInfoAsync : QueryCommandBaseAsync<int, int>
{
    public struct InParameters : IDbInParameters
    {
        public long accountId  { get; init; }
        
        public int lastWorldId { get; init; }
        public int lastZoneId { get; init; }
        public float positionX { get; init; }
        public float positionY { get; init; }
        public float positionZ { get; init; }
    }
    
    private const string _UpdatePlayerInfo = "UPDATE player_info SET last_world_id = @lastWorldId, " +
                                             "last_zone_id = @lastZoneId, " +
                                             "position_x = @positionX, position_y = @positionY, " +
                                             "position_z = @positionZ, " +
                                             "update_date = CURRENT_TIMESTAMP " +
                                             "WHERE account_id = @accountId";

    public UpdatePlayerInfoAsync(MySqlDapperServiceBase dbContext, MySqlTransaction transaction = null) 
        : base(dbContext, transaction)
    {
        
    }

    public override async Task<int> ExecuteQueryAsync(IDbInParameters inParameters)
    {
        if (inParameters is not InParameters inParams)
            throw new DbContextException(DbErrorCode.InParameterWrongType, $"[{GetType().Name}] Parameter Type is wrong");

        var parameters = new
                         {
                             inParams.lastWorldId, inParams.lastZoneId,
                             inParams.positionX,
                             inParams.positionY, inParams.positionZ, inParams.accountId
                         };
        
        var result = await _RunQueryReturnModelAsync(_UpdatePlayerInfo, parameters);
        return result.FirstOrDefault();
    }
}