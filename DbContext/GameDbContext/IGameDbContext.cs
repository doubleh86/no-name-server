using DbContext.GameDbContext.DbResultModel;

namespace DbContext.GameDbContext;

public interface IGameDbContext : IDisposable
{
    Task<PlayerInfoResult> GetPlayerInfoAsync(long accountId);
    Task<int> ItemUseAsync(long accountId, int itemId, int itemCount);
    Task<int> AutoSaveInfoAsync(PlayerInfoResult playerInfoResult);
}
