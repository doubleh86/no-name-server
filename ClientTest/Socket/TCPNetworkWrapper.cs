using ClientTest.Handlers;
using ClientTest.Socket.TCPClient;
using CommonData.NetworkModels.CommonCommand;

namespace ClientTest.Socket;

public class TCPNetworkWrapper
{
    private ITCPSession _tcpSession;
    private Thread _receiveThread;
    private CancellationTokenSource _cancellationTokenSource;

    private readonly ulong _accountId;
    private readonly TCPPacketHandler _packetReceiveHandler;
    private readonly string _serverIp;
    private readonly int _serverPort;


    public ITCPSession GetTcpSession() => _tcpSession;
    
        
    public TCPNetworkWrapper(string serverIp, int serverPort, ulong accountId, TCPPacketHandler packetReceiveHandler)
    {
        _serverIp = serverIp;
        _serverPort = serverPort;
        _accountId = accountId;

        _packetReceiveHandler = packetReceiveHandler;
        _packetReceiveHandler.Initialized();
    }

    public void ConnectServer()
    {
        if (_tcpSession != null && _tcpSession.IsConnected() == true)
            return;

        if (_tcpSession != null)
        {
            _tcpSession.Disconnect(SessionCloseReason.Reconnecting);
            _tcpSession.Dispose();    
        }
        
        var tcpConnector = new TCPConnector();
        tcpConnector.ConnectionCompleteHandler = session =>
        {
            if (session.Identifier != _accountId)
            {
                Console.WriteLine($"Account Id is not same [{session.Identifier}][{_accountId}]");
                return;
            }
                
            _tcpSession = session;
            _tcpSession.ConnectComplete();
            _tcpSession.Identifier = _accountId;
        };
            
        _ = tcpConnector.Connect(_serverIp, _serverPort, 0, _accountId);

        _cancellationTokenSource = new CancellationTokenSource();
        
        _receiveThread = new Thread(_ReceiveThread);
        _receiveThread.Start();
            
        Console.WriteLine($"[{_receiveThread.ManagedThreadId}]");

    }

    private void _ReceiveThread()
    {
        while (_cancellationTokenSource.Token.IsCancellationRequested == false)
        {
            if (_tcpSession == null || _tcpSession.IsConnected() == false)
            {
                Thread.Sleep(10);
                continue;
            }

            if (_tcpSession.HasData() == false)
            {
                Thread.Sleep(10);
                continue;
            }
    
            var receiveDataList = _tcpSession.GetQueueData();
            foreach (var data in receiveDataList)
            {
                if (data.Key == PongCommand.PongCommandId)
                {
                    OnPong(data);
                    continue;
                }
    
                var handler = _packetReceiveHandler.GetHandler(data.Key);
                if (handler == null)
                {
                    Console.WriteLine($"Not Register message Id : {data.Key}");
                    continue;
                }
                    
                handler(data);
            }
                
            Thread.Sleep(10);
        }
    }

    private void OnPong(NetworkPackage data)
    {
        _tcpSession.ReceivePong();
    }

    public void Disconnect()
    {
        _tcpSession?.Disconnect(SessionCloseReason.Disconnecting);
        
        _cancellationTokenSource.Cancel();
        _receiveThread?.Join();
    }
}