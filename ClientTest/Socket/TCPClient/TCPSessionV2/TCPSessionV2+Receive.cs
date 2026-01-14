using System.Net.Sockets;

namespace ClientTest.Socket.TCPClient.TCPSessionV2;

public partial class TCPSessionV2
{
        
    private readonly LAQueue<NetworkPackage> _receiveQueue = new();
    private byte[] _receiveBuffer = new byte[TCPCommon.MaxReceivePacketSize * TCPCommon.BufferFactor];
    private int _receiveWriteOffset;

    private void _ReceiveMemberClear()
    {
        _receiveQueue.Clear();
        _receiveWriteOffset = 0;
    }

    private async Task ReceiveLoopAsync(CancellationToken token)
    {
        try
        {
            while (token.IsCancellationRequested == false && IsConnected() == true)
            {
                int freeSpace = _receiveBuffer.Length - _receiveWriteOffset;
                if (freeSpace < NetworkPackage.HeaderSize)
                {
                    if (_receiveWriteOffset >= _receiveBuffer.Length)
                    {
                        Array.Resize(ref _receiveBuffer, _receiveBuffer.Length * 2);
                    }
                    
                    freeSpace = _receiveBuffer.Length - _receiveWriteOffset;
                }
                var receivedbuffer = new Memory<byte>(_receiveBuffer, _receiveWriteOffset, freeSpace);
                var received = await _socket.ReceiveAsync(receivedbuffer, SocketFlags.None, token);
                if (received <= 0)
                {
                    Disconnect(SessionCloseReason.ServerClosing);
                    break;
                }

                _receiveWriteOffset += received;
                _ProcessPackets();
            }
        }
        catch (OperationCanceledException)
        {
            Console.WriteLine("ReceiveLoopAsync OperationCanceledException");
        }
        catch (Exception e)
        {
            Console.WriteLine(e);
            Disconnect(SessionCloseReason.ReceiveProtocolError); // 세션 종료 처리 추가
        }
    }

    private void _ProcessPackets()
    {
        try
        {
            var readOffset = 0;
            while (_receiveWriteOffset - readOffset >= NetworkPackage.HeaderSize)
            {
                var bodySize = BitConverter.ToInt32(_receiveBuffer, readOffset);
                var key = BitConverter.ToInt32(_receiveBuffer, readOffset + sizeof(int));

                if (_receiveWriteOffset - readOffset < bodySize + NetworkPackage.HeaderSize)
                {
                    break;
                }
                
                var networkPackage = new NetworkPackage
                {
                    BodySize = bodySize,
                    Key = key
                };
                
                networkPackage.SetData(_receiveBuffer, readOffset + NetworkPackage.HeaderSize, networkPackage.BodySize);
                _PushReceiveQueue(networkPackage);
                readOffset += networkPackage.BodySize + NetworkPackage.HeaderSize;
            }

            if (readOffset > 0)
            {
                var remainingSize = _receiveWriteOffset - readOffset;
                if (remainingSize > 0)
                {
                    Buffer.BlockCopy(_receiveBuffer, readOffset, _receiveBuffer, 0, remainingSize);
                }
                
                _receiveWriteOffset = remainingSize;
                
            }
        }
        catch (Exception e)
        {
            Console.WriteLine(e);
            throw;
        }
    }
    
}