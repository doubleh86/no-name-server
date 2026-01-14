using System.Net.Sockets;

namespace ClientTest.Socket.TCPClient.TCPSessionV1;

public partial class TCPSession
{
        
    private readonly LAQueue<NetworkPackage> _receiveQueue = new();
    private readonly byte[] _receiveBuffer = new byte[TCPCommon.MaxReceivePacketSize * TCPCommon.BufferFactor];
    private int _receiveWriteOffset;

    private void _ReceiveMemberClear()
    {
        _receiveQueue.Clear();
        _receiveWriteOffset = 0;
    }
        
    private void _StartReceive()
    {
        if (_state != ITCPSession.SessionState.Connected)
            return; 
            
        _socket.BeginReceive(_receiveBuffer, _receiveWriteOffset, TCPCommon.MaxReceivePacketSize, SocketFlags.None, _ReceiveComplete, this);
    }
        
    private void _ReceiveComplete(IAsyncResult ar)
    {
        var tcpSession = ar.AsyncState as TCPSession;
        if (tcpSession == null || tcpSession._socketUdid != _socketUdid)
        {
            Console.WriteLine($"Tcp socket is null or udid is not same [{_socketUdid}][{tcpSession?._socketUdid}]");
            tcpSession?.GetSocket().EndReceive(ar);
            Disconnect(SessionCloseReason.ApplicationError);
            return;
        }
        
        if (tcpSession.GetSocket() == null || tcpSession._state != ITCPSession.SessionState.Connected)
        {
            Console.WriteLine("Tcp socket already closed");
            return;
        }

        try
        {
            int length = tcpSession.GetSocket().EndReceive(ar);
            if (length <= 0)
            {
                Disconnect(SessionCloseReason.ServerClosing);
                return;
            }

            int totalBufferSize = _receiveWriteOffset + length;

            int readOffset = 0;
            while (totalBufferSize - readOffset >= NetworkPackage.HeaderSize)
            {
                var networkPackage = new NetworkPackage
                {
                    BodySize = BitConverter.ToInt32(_receiveBuffer, readOffset),
                    Key = BitConverter.ToInt32(_receiveBuffer, readOffset + sizeof(int))
                };

                if (totalBufferSize - readOffset < networkPackage.BodySize + NetworkPackage.HeaderSize)
                {
                    break;
                }


                networkPackage.SetData(_receiveBuffer, readOffset + NetworkPackage.HeaderSize, networkPackage.BodySize);
                _PushReceiveQueue(networkPackage);
                readOffset += networkPackage.BodySize + NetworkPackage.HeaderSize;
            }

            _receiveWriteOffset = totalBufferSize - readOffset;

            if (readOffset > 0)
            {
                Buffer.BlockCopy(_receiveBuffer, readOffset, _receiveBuffer, 0, _receiveWriteOffset);
            }

            _StartReceive();

        }
        catch (SocketException e)
        {
            if (e.ErrorCode == (int)SocketError.Shutdown)
                return;

            Console.WriteLine($"[_ReceiveComplete][{e.ErrorCode}][{e.Message}]");
            Disconnect(SessionCloseReason.ReceiveProtocolError);
        }
        catch (ObjectDisposedException e)
        {
            Console.WriteLine("Dispose object exception : " + e.Message);
            Disconnect(SessionCloseReason.ApplicationError);
        }
        catch (Exception e)
        {
            Console.WriteLine($"[_ReceiveComplete][{e.Message}][{e.StackTrace}]");
            Disconnect(SessionCloseReason.Unknown);
        }
    }
}