namespace ClientTest.Socket.TCPClient;


public interface ITCPSession : IDisposable
{
    public enum SessionState
    {
        None,
        Connected,
        Disconnected
    }
    
    ulong Identifier { get; set; }
    
    bool IsConnected();
    void Disconnect(SessionCloseReason reason = SessionCloseReason.Unknown);
    void ConnectComplete();
    bool HasData();
    void ReceivePong();
    void Send(NetworkPackage networkPackage);

    System.Net.Sockets.Socket GetSocket();

    List<NetworkPackage> GetQueueData();

}