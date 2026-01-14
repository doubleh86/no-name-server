using System.Net.Sockets;

namespace ClientTest.Socket.TCPClient.TCPSessionV1;

public partial class TCPSession : ITCPSession
{
    private readonly System.Net.Sockets.Socket _socket;
    private readonly string _socketUdid;
        
    private ITCPSession.SessionState _state = ITCPSession.SessionState.None;
    private readonly Lock _disconnectLock = new();
        
    public System.Net.Sockets.Socket GetSocket() => _socket;
    public string GetUdid() => _socketUdid;
    
    public ulong Identifier { get; set; }
 
    public TCPSession(System.Net.Sockets.Socket socket)
    {
        _socket = socket;
        _socketUdid = Guid.NewGuid().ToString();

        _Initialize();
    }

    private void _Initialize()
    {
        _tempPacketBuff.Clear();
        _ReceiveMemberClear();
        _SendMemberClear();
        _ServiceMemberClear();
        _state = ITCPSession.SessionState.None;
    }

    public void ConnectComplete()
    {
        _lastReceivedPongTime = _GetUtcTimeStampSeconds();
        _state = ITCPSession.SessionState.Connected;
        _SetConnectPacket(true, 0, "");

        _isRunKeepAlive = true;
        _keepAliveThread = new Thread(_KeepAliveThread)
        {
            IsBackground = true
        };
        
        _keepAliveThread.Start();
        _StartReceive();
    }


    public bool IsConnected()
    {
        if (_socket == null || _socket.Connected == false)
            return false;

        return _state == ITCPSession.SessionState.Connected;
    }
        
    public void Disconnect(SessionCloseReason reason)
    {
        Console.WriteLine($"Disconnect reason: [{reason}][{_pingTryCount}]");

        lock (_disconnectLock)
        {
            if (_state == ITCPSession.SessionState.Disconnected)
                return;
            try
            {
                if (_state == ITCPSession.SessionState.Connected)
                {
                    _SetClosePacket(reason);
                }

                if (_socket != null && _socket.Connected == true)
                {
                    _socket.Shutdown(SocketShutdown.Both);
                    _socket.Disconnect(false);
                }
                 
            }
            finally
            {
                if(_socket != null)
                    _socket.Close();
                
                _state = ITCPSession.SessionState.Disconnected;
                _isRunKeepAlive = false;
            }    
        }
    }
    

    public void Dispose()
    {
        if(IsConnected() == true)
            Disconnect(SessionCloseReason.Disconnecting);
            
        _Initialize();
        _socket?.Close();
        _socket?.Dispose();
    }

    public static eReconnectType GetReconnectType(SessionCloseReason reason)
    {
        switch (reason)
        {
            case SessionCloseReason.Unknown:
            case SessionCloseReason.Disconnecting:
                return eReconnectType.None;
                
            case SessionCloseReason.ServerShutdown:
            case SessionCloseReason.SendProtocolError:
            case SessionCloseReason.ReceiveProtocolError:
            case SessionCloseReason.Timeout:
                return eReconnectType.NewServerReconnect;
                
            case SessionCloseReason.ApplicationError:
            case SessionCloseReason.ServerClosing:
                return eReconnectType.SameServerReconnect;
            default:
                return eReconnectType.None;
        }
    }
        
}