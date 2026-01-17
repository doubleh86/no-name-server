using System.Collections.Concurrent;
using MySqlX.XDevAPI;
using SuperSocket.Connection;
using WorldServer.Network;
using SessionState = SuperSocket.Server.Abstractions.SessionState;

namespace WorldServer.Services;

public class UserService : IDisposable
{
    private readonly ConcurrentDictionary<long, UserSessionInfo> _sessions = new();

    public static bool IsSessionAlive(UserSessionInfo session)
    {
        return session != null &&
               session.State != SessionState.None &&
               session.State != SessionState.Closed;
    }
    public void AddUser(UserSessionInfo info)
    {
        _sessions.TryAdd(info.Identifier, info);
    }

    public void RemoveUser(long identifier)
    {
        _sessions.TryRemove(identifier, out _);
    }

    public void Dispose()
    {
        foreach (var session in _sessions.Values)
        {
            try
            {
                // 대기 안한다.
                _ = session.CloseAsync(CloseReason.ServerShutdown); 
            }
            catch (Exception ex)
            {
                // 로거가 있다면 로그 기록
                Console.WriteLine($"Error closing session {session.SessionID}: {ex.Message}");
            }
        }

        _sessions.Clear();
    }
}