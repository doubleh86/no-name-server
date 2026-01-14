using ServerFramework.SqlServerServices.Models;

namespace DbContext.GameDbContext;

public static class GameDbContextWrapper
{
    private static bool _isMySql = false;
    public static IGameDbContext Create()
    {
        return _isMySql == true ? MySqlGameDbContext.Create() : null;
    }
    
    public static void SetDefaultServerInfo(SqlServerDbInfo settings)
    {
        _isMySql = settings.IsMySql;
        if (_isMySql == true)
        {
            MySqlGameDbContext.SetDefaultServerInfo(settings);
            return;
        }
        
        return;
    }
}