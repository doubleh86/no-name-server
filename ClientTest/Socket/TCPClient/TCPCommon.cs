namespace ClientTest.Socket.TCPClient;

public enum SessionCloseReason
{
    Unknown = 0,
    Disconnecting = 1,
    ServerShutdown = 2,
    SendProtocolError = 3,
    ReceiveProtocolError = 4,
    ApplicationError = 5,
    Timeout = 6,
    ServerClosing = 7,
    Reconnecting = 8,
}
public enum eReconnectType
{
    None = 0,
    SameServerReconnect = 1,
    NewServerReconnect = 2,
}
public static class TCPCommon
{
    public const int MaxReceivePacketSize = 65536;
    public const int BufferFactor = 10;
    public const int MaxSendPacketSize = 65536;
    public const int MaxRecvPopCount = 4;
    public const int TimeoutSeconds = 60 * 5;
    public const int SendPingSeconds = 30;
}