using ClientTest.Helpers;
using CommonData.NetworkModels.CommonCommand;
using NetworkProtocols.Socket;

namespace ClientTest.Socket.TCPClient.TCPSessionV1;

public partial class TCPSession
{
	private readonly List<NetworkPackage> _tempPacketBuff = [];
	private bool _isRunKeepAlive = false;

	private long _lastReceivedPongTime = 0;
	private long _lastSendPingTime = 0;
	private int _pingTryCount = 0;
	
	private DateTime _lastSendPingTimeUtc = DateTime.MinValue;
		
	private Thread _keepAliveThread;
		
	private void _ServiceMemberClear()
	{
		_isRunKeepAlive = false;
		_lastReceivedPongTime = 0;
		_lastSendPingTime = 0;
		_keepAliveThread?.Abort();
		_keepAliveThread?.Join();
			
		Interlocked.Exchange(ref _pingTryCount, 0);
	}
	public void ReceivePong()
	{
		_lastReceivedPongTime = _GetUtcTimeStampSeconds();
		Interlocked.Exchange(ref _pingTryCount, 0);
		
		var currentTime = DateTime.UtcNow;
		var timeSpan = currentTime - _lastSendPingTimeUtc;
		Console.WriteLine($"Ping Time [{timeSpan.TotalMilliseconds}] ms");
	}
		
		
	private void _KeepAliveThread()
	{
		_isRunKeepAlive = true;
		_SendPing();
			
		while (_isRunKeepAlive)
		{
			var currentTimeStamp = _GetUtcTimeStampSeconds();
			if (currentTimeStamp - _lastSendPingTime > TCPCommon.TimeoutSeconds || _pingTryCount > 3)
			{
				if (_pingTryCount > 3)
				{
					Console.WriteLine($"Ping Try Count Over 3 [{currentTimeStamp - _lastReceivedPongTime}]");		
				}
					
				Disconnect(SessionCloseReason.Timeout);
				continue;
			}

			if (currentTimeStamp - _lastReceivedPongTime > TCPCommon.SendPingSeconds)
			{
				_SendPing();
			}

			Thread.Sleep(2000);
		}
	}
	private async Task AsyncKeepAlive()
	{
		_isRunKeepAlive = true;
		_SendPing();

		while (_isRunKeepAlive)
		{
			await Task.Delay(2000);
			var currentTimeStamp = _GetUtcTimeStampSeconds();

			if (currentTimeStamp - _lastSendPingTime > TCPCommon.TimeoutSeconds || _pingTryCount > 3)
			{
				Disconnect(SessionCloseReason.Timeout);
				continue;
			}

			if (currentTimeStamp - _lastReceivedPongTime > TCPCommon.SendPingSeconds)
			{
				_SendPing();
				Console.WriteLine("Send Ping");
			}
		}
	}

	private void _SendPing()
	{
		var command = new PingCommand
		{
			SendTimeMilliseconds = _GetUtcTimeStampSeconds()
		};

		var body = MemoryPackHelper.Serialize(command);
		var packet = TCPNetworkHelper.MakePackage(PingCommand.PingCommandId, body);
		
		Send(packet);
		
		Interlocked.Increment(ref _pingTryCount);
		_lastSendPingTime = _GetUtcTimeStampSeconds();
		_lastSendPingTimeUtc = DateTime.UtcNow;
	}
	public List<NetworkPackage> GetQueueData()
	{
		_tempPacketBuff.Clear();

		if (_receiveQueue.Count <= 0)
			return _tempPacketBuff;

		var loopCount = 0;
		while (_receiveQueue.Count > 0 && loopCount < TCPCommon.MaxRecvPopCount)
		{
			_tempPacketBuff.Add(_receiveQueue.Dequeue());
			++loopCount;
		}

		return _tempPacketBuff;
	}

	public bool HasData()
	{
		return (_receiveQueue.Count > 0);
	}

	private void _PushReceiveQueue(NetworkPackage data)
	{
		_receiveQueue.Enqueue(data);
	}

	private void _PushSendQueue(byte[] data, int startOffset, int endOffset)
	{
		byte[] newMessage = new byte[endOffset - startOffset];
		Buffer.BlockCopy(data, startOffset, newMessage, 0, endOffset - startOffset);
		_sendQueue.Enqueue(newMessage);
	}


	private void _PushReceiveQueueFirst(NetworkPackage data)
	{
		_receiveQueue.EnqueueFirst(data);
	}

	private void _SetConnectPacket(bool connected, int code, string msg)
	{
		var command = new ConnectedCommand
		{
			Code = code,
			Message = msg,
			IsSuccess = connected
		};

		var body = MemoryPackHelper.Serialize(command);
		var packet = TCPNetworkHelper.MakePackage(ConnectedCommand.ConnectedCommandId, body);
		_PushReceiveQueueFirst(packet);
	}

	private void _SetClosePacket(SessionCloseReason reason, string message = "disconnected")
	{
		var command = new DisconnectedCommand
		{
			Identifier = 0,
			Message = message,
			Reason = (int)reason
		};
		
		var body = MemoryPackHelper.Serialize(command);
		var packet = TCPNetworkHelper.MakePackage(DisconnectedCommand.DisconnectedCommandId, body);
		_PushReceiveQueueFirst(packet);
	}

	private static long _GetUtcTimeStampSeconds()
	{
		return DateTimeOffset.UtcNow.ToUnixTimeSeconds();
	}

		
}