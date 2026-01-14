using System.Net.Sockets;

namespace ClientTest.Socket.TCPClient.TCPSessionV1;

public partial class TCPSession
{
    private readonly LAQueue<byte[]> _sendQueue = new LAQueue<byte[]>(); 
    private readonly object _sendLock = new LAQueue<byte[]>();
    private readonly byte[] _sendBuffer = new byte[TCPCommon.MaxSendPacketSize];
    private bool _sendPending = false;
    private int _sendWriteOffset = 0;
    private int _sendCompleteOffset = 0;
    private bool _isSending = false;
    private int _sendFailCount = 0;

    private bool _sendAsync = false;

    private void _SendMemberClear()
    {
        lock (_sendLock)
        {
            _sendQueue.Clear();
            _sendCompleteOffset = 0;
            _sendWriteOffset = 0;
            _isSending = false;
            _sendPending = false;
            _sendFailCount = 0;
        }
    }
    
    public void Send(NetworkPackage networkPackage)
    {
        if (_sendAsync == true)
        {
            SendAsync(networkPackage);
            return;
        }
                
        var sendBytes = new byte[NetworkPackage.HeaderSize + networkPackage.BodySize];
        var sizeBytes = BitConverter.GetBytes(networkPackage.BodySize);
        var messageIdBytes = BitConverter.GetBytes(networkPackage.Key);
            
        Buffer.BlockCopy(sizeBytes, 0, sendBytes, _sendWriteOffset, sizeBytes.Length);
        Buffer.BlockCopy(messageIdBytes, 0, sendBytes, _sendWriteOffset + sizeof(int), messageIdBytes.Length);
        Buffer.BlockCopy(networkPackage.Body, 0, sendBytes, _sendWriteOffset + NetworkPackage.HeaderSize, networkPackage.BodySize);

        _socket.Send(sendBytes);
    }
        
    public void SendAsync(NetworkPackage networkPackage)
    {
        var sizeBytes = BitConverter.GetBytes(networkPackage.BodySize);
        var messageIdBytes = BitConverter.GetBytes(networkPackage.Key);
            
        lock (_sendLock)
        {
            int sendSize = networkPackage.BodySize + NetworkPackage.HeaderSize;
            if (_sendPending == true || _sendWriteOffset + sendSize > TCPCommon.MaxSendPacketSize)
            {
                _sendPending = true;
                _SetSendPendingData(sendSize, networkPackage);
                return;
            }
                
            Buffer.BlockCopy(sizeBytes, 0, _sendBuffer, _sendWriteOffset, sizeBytes.Length);
            Buffer.BlockCopy(messageIdBytes, 0, _sendBuffer, _sendWriteOffset + sizeof(int), messageIdBytes.Length);
            Buffer.BlockCopy(networkPackage.Body, 0, _sendBuffer, _sendWriteOffset + NetworkPackage.HeaderSize, networkPackage.BodySize);

            _sendWriteOffset += sendSize;
            
            if(_isSending == true)
            {
                Console.WriteLine($"Send() isSending [{_sendWriteOffset}][{_sendCompleteOffset}][{_sendFailCount}]");
                return;
            }
                    
            try
            {
                _StartSend("Send()");
            }
            catch (SocketException e)
            {
                Console.WriteLine($"[Send][{e.ErrorCode}][{e.Message}]");
                Disconnect(SessionCloseReason.SendProtocolError);
            }
            catch (Exception e)
            {
                Console.WriteLine($"[Send][{e.Message}]");
                Disconnect(SessionCloseReason.SendProtocolError);
            }
        }
    }

    private void _StartSend(string methodName = "")
    {
        if (IsConnected() == false)
            return;
            
        var sendSize = _sendWriteOffset - _sendCompleteOffset;

        try
        {
            Console.WriteLine($"StartSend : {methodName}|{_sendWriteOffset}|{_sendCompleteOffset}|{sendSize}");
            _socket.BeginSend(_sendBuffer, _sendCompleteOffset, sendSize, SocketFlags.None, _SendComplete, this);
            _isSending = true;
        }
        catch (Exception e)
        {
            Console.WriteLine($"Send() Exception : {e.Message}");
            throw;
        }
            
    }

    private void _SendComplete(IAsyncResult ar)
    {
        var tcpSession = ar.AsyncState as TCPSession;
        if (tcpSession == null || tcpSession.IsConnected() == false)
        {
            Console.WriteLine("Send Complete Session is null");
            return; 
        }

        if (tcpSession._socketUdid != _socketUdid)
        {
            Console.WriteLine($"Send Complete Session is not same [{tcpSession._socketUdid}][{_socketUdid}]");
            tcpSession._socket.EndSend(ar);
            Disconnect(SessionCloseReason.ApplicationError);

            return;
        }
            
        lock (_sendLock)
        {
            try
            {
                    
                int length = tcpSession.GetSocket().EndSend(ar);
                _sendCompleteOffset += length;
                _sendFailCount = 0;
                    
                if (_sendWriteOffset != _sendCompleteOffset)
                {
                    Console.WriteLine($"Start Remain Data Send: {_sendWriteOffset}|{_sendCompleteOffset}");
                    _StartSend($"_SendComplete_1({_sendWriteOffset}, {_sendCompleteOffset})");
                    return;
                }

                _sendCompleteOffset = 0;
                _sendWriteOffset = 0;
                _isSending = false;
                    
                Console.WriteLine($"Send Complete {_sendPending}|[{_sendCompleteOffset}|{_sendWriteOffset}]|{_sendQueue.Count}|{_isSending}");

                if (_sendPending == false)
                    return;

                if (_sendQueue.Count < 1)
                {
                    _sendPending = false;
                }

                var data = _sendQueue.Dequeue();
                if (data.Length <= 0)
                {
                    // Wrong packet 무시 한다.
                    return;
                }
                    
                Console.WriteLine("Send Pending Queue Data");

                Buffer.BlockCopy(data, 0, _sendBuffer, _sendWriteOffset, data.Length);
                _sendWriteOffset += data.Length;
                
                _StartSend($"_SendComplete_2({_sendQueue.Count})");

            }
            catch (SocketException e)
            {
                Console.WriteLine($"[_SendComplete][{e.ErrorCode}][{e.Message}]");
                if (e.ErrorCode == (int)SocketError.Shutdown)
                    return;
                    
                Disconnect(SessionCloseReason.SendProtocolError);
            }
            catch (Exception e)
            {
                Console.WriteLine($"[_ReceiveComplete][{e.Message}]");
                Disconnect(SessionCloseReason.SendProtocolError);
            }
        }
    }

    private void _SetSendPendingData(int size, NetworkPackage networkPackage)
    {
        var buffer = new byte[size];
        var sizeBytes = BitConverter.GetBytes(networkPackage.BodySize);
        var messageIdBytes = BitConverter.GetBytes(networkPackage.Key);
            
        Buffer.BlockCopy(sizeBytes, 0, buffer, 0, sizeBytes.Length);
        Buffer.BlockCopy(messageIdBytes, 0, buffer, sizeof(int), sizeBytes.Length);
        Buffer.BlockCopy(networkPackage.Body, 0, buffer, NetworkPackage.HeaderSize, networkPackage.BodySize);
            
        _PushSendQueue(buffer, 0, size);

        if (_sendPending == true && _isSending == true && _sendQueue.Count > 0)
        {
            _isSending = false;
            _StartSend("_SetSendPendingData");
        }
            
    }
}