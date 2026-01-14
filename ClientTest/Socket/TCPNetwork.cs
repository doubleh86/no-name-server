// #define _NETWORK_TEST_
//
// using System;
// using System.Collections.Generic;
// using System.Net;
// using System.Net.Sockets;
// using System.Threading;
// using System.Threading.Tasks;
// using MessagePack;
// using R1B_Packet.TcpProtocols;
// using TestBot.Utils;
//
// // ReSharper disable IdentifierTypo
// // ReSharper disable FieldCanBeMadeReadOnly.Local
//
// // ReSharper disable InconsistentNaming
//
// namespace TestBot.Network
// {
//     public class CustomAsyncState
//     {
//         public Socket sock;
//         public string uuid;
//     }
//
//     public class TCPNetwork
//     {
//         public const int MAX_PACKET_SIZE = 32767;
//         public const int BUFFER_FACTOR = 64;
//         // 한번에 얻어가는 패킷 개수 (프로젝트 또는 패킷 처리 구조 마다 적정한 값을 찾아 변경)
//         public const int MAX_RECV_POP_COUNT = 4;
//
//         static private int TIMEOUT_SECONDS = 10;
//
//         private System.Net.Sockets.Socket _sock = null;
//         //private float timer = 0.0f;
//         //private int counter = 0;
//         private readonly byte[] _recv_buffer = new byte[MAX_PACKET_SIZE * BUFFER_FACTOR];
//         private readonly byte[] _send_buffer = new byte[MAX_PACKET_SIZE];
//         private readonly LAQueue<Packet> _recv_que = new LAQueue<Packet>();
//         private readonly LAQueue<byte[]> _send_que = new LAQueue<byte[]>();
//
//         private int _send_complete_offset = 0;
//         private int _send_write_offset = 0;
//
//         private int _recv_write_offset = 0;
//         private int _recv_fail_count = 0;
//
//         private object _send_lock = new object();
//         private bool _is_send = false;
//         private bool _send_pending = false;
//         private string _sock_uuid = "";
//         private bool _connected = false;
//         private CustomAsyncState _state = null;
//         private ManualResetEvent _connect_timeout_event = null;
//         private bool _is_connecting = false;
//         private bool _client_ping = false;
//         private long _last_recv_time = 0;
//
//         private bool _run_recv_checker = true;
//
//         private List<Packet> _tempPacketBuff = new List<Packet>();
//
// #if _MISSING_ENDSEND_TEST_
//         private int _complete_send_count = 0;
// #endif
// #if _DUPLICATED_SOCKET_TEST_
// 		private int _test_count = 2;
// 		private string _test_ip;
// 		private int _test_port;
// 		private System.Net.Sockets.Socket _test_sock = null;
// #endif
//
//         public bool IsAvailable => _sock != null;
//
//         public TCPNetwork()
//         {
//             _connect_timeout_event = new ManualResetEvent(false);
//         }
//
//         private void Initialize()
//         {
//             _send_complete_offset = 0;
//             _send_write_offset = 0;
//             _recv_write_offset = 0;
//             _recv_fail_count = 0;
//             _client_ping = false;
//             _is_send = false;
//
//             _recv_que.Clear();
//             _send_que.Clear();
//         }
//
//         public async Task<bool> Connect(string ip, int port, int timeout_seconds)
//         {
//             TIMEOUT_SECONDS = timeout_seconds;
//
//             if (_sock != null)
//             {
//                 Console.WriteLine("Socket already has a connection");
//                 return true;
//             }
//
//             if (_is_connecting)
//             {
//                 Console.WriteLine("Socket already try to connect");
//                 return true;
//             }
//
//
//             Initialize();
//
//             if (false == IPAddress.TryParse(ip, out var address))
//             {
//                 try
//                 {
//                     address = Dns.GetHostEntry(ip).AddressList[0];
//                 }
//                 catch
//                 {
//                     return false;
//                 }
//             }
//
//             _sock = new System.Net.Sockets.Socket(address.AddressFamily,
//                 SocketType.Stream,
//                 ProtocolType.Tcp);
//
//             _sock_uuid = System.Guid.NewGuid().ToString();
//
//             _state = new CustomAsyncState { sock = _sock, uuid = _sock_uuid };
//
//
//             _is_connecting = true;
//             _connect_timeout_event.Reset();
//
//             //string ip = "192.168.10.9";
//             //int port = 8194;
//             _sock.BeginConnect(ip, port, new AsyncCallback(ConnectComplete), _state);
//
//             // await TCPNetwork.AsyncWaitConnect(this);
//
// #if _DUPLICATED_SOCKET_TEST_
// 			if (_test_count > 0) {
// 				//_test = false;
// 				//Shutdown (_sock);
// 				//_sock_uuid = "";
// 				_test_ip = ip;
// 				_test_port = port;
// 				_test_sock = new System.Net.Sockets.Socket(address.AddressFamily,
// 					SocketType.Stream,
// 					ProtocolType.Tcp);
// 				//Debug.LogErrorFormat ("Socket Connected {0}", _sock.Connected);
// 			}
// #endif
//
//             return true;
//         }
//
//         private static async Task AsyncWaitConnect(TCPNetwork tcp)
//         {
//             Task delay = Task.Delay(TCPNetwork.TIMEOUT_SECONDS * 1000);
//             await delay;
//             //Debug.LogError("await 풀림");
//             if (false == tcp._connect_timeout_event.WaitOne(10, false))
//             {
//                 tcp._is_connecting = false;
//                 if (false == tcp._connected)
//                 {
//                     tcp.SetClosePacket();
// #if _NETWORK_TEST_
//                     tcp.Shutdown(tcp._sock, true, 1);
// #else
//     			    tcp.Shutdown (_sock);
// #endif       
//                 }
//             }
//             else
//             {
//                 LoggerHelper.Instance.LogInformation("Connect Complete");
//             }
//         }
//
//         private async Task AsyncRecvChecker()
//         {
//             _run_recv_checker = true;
//
//             // 소켓 연결 후 바로 ping 을 한번 날린다
//             _client_ping = true;
//
//             // Data.Manager.UpdateSourceData(new Data.Source.SourcePingSend() { SendTimeMilliseconds = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds() });
//             //
//             // Send(PING_TO_SERVER.Make(PacketType.CLIENT_PING));
//
//             while (_run_recv_checker)
//             {
//                 Task delay = Task.Delay(TIMEOUT_SECONDS * 1000 / 2);
//                 await delay;
//                 //Debug.Log("RECV CHECKER");
//
//                 long now_timestamp = GetUtcTimeStamp();
//                 if (now_timestamp - _last_recv_time > 40 && _client_ping)
//                 {
//                     // 접속 종료
//                     Console.WriteLine("Cannot Recv any packet");
//                     SetClosePacket();
//
// #if _NETWORK_TEST_
//                     Shutdown(_sock, true, 13);
// #else
// 					Shutdown (_sock);
// #endif              
//                 }
//                 else if ((now_timestamp - _last_recv_time > 20) && (false == _client_ping))
//                 {
//                     // ping 날린다    
//                     _client_ping = true;
//
//                     // Data.Manager.UpdateSourceData(new Data.Source.SourcePingSend() { SendTimeMilliseconds = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds() });
//                     //
//                     // if (IsAvailable)
//                     //     Send(PING_TO_SERVER.Make(PacketType.CLIENT_PING));
//                     //Debug.LogWarning("Send Client Ping");
//                 }
//             }
//         }
//
//
//         public void Disconnect()
//         {
//             if (_sock == null)
//                 return;
//             SetClosePacket();
// #if _NETWORK_TEST_
//             Shutdown(_sock, true, 1);
// #else
// 			Shutdown (_sock);
// #endif
//
//         }
//
//         public bool Connected()
//         {
//             if (_sock == null)
//                 return false;
//
//             // deprecate : Socket 객체의 Connected가 상태 표현 오류 발생 가능성
//             // bool connected = false;
//             // try
//             // {
//             //     connected = _sock.Connected;
//             // }
//             // catch
//             // {
//
//             // }
//
//             return _connected;
//         }
//
//         private void ConnectComplete(IAsyncResult ar)
//         {
//             CustomAsyncState state = ar.AsyncState as CustomAsyncState;
//             System.Net.Sockets.Socket target_socket = state.sock;
//             if (target_socket == null)
//             {
//                 Console.WriteLine("Debug Message(ConnectComplete) : null socket");
//                 return;
//             }
//
// #if _DUPLICATED_SOCKET_TEST_
//             Debug.LogErrorFormat ("Socket Connected {0}", state.sock.Connected);
// 			if (_test_count> 0) 
// 			{
// 				//Shutdown (_sock);
// 				if (_test_count == 2) {
// 					_test_sock.BeginConnect (_test_ip, _test_port, new AsyncCallback (ConnectComplete), _state);
// 					_test_sock.EndConnect (ar);
// 				}
//
// 				byte[] newMessage = SetConnectPacket(true, 0, "");
// 				PushRecvQueue(newMessage);
// 				target_socket.BeginReceive(recvBuffer,
// 					0,
// 					MAX_PACKET_SIZE,
// 					SocketFlags.None,
// 					new AsyncCallback(RecvComplete),
// 					state);
//
// 				--_test_count;
// 				_connected = true;
// 			
// 				return;
// 			}
// #endif
//
//             try
//             {
//                 target_socket.EndConnect(ar);
//                 if (state.uuid != _sock_uuid)
//                 {
//                     Console.WriteLine("Debug Message(ConnectComplete) : Previous socket 2");
// #if _NETWORK_TEST_
//                     Shutdown(target_socket, false, 10);
// #else
//                 Shutdown (target_socket, false);
// #endif
//                     target_socket = null;
//                     return;
//                 }
//
//                 if (target_socket.Connected)
//                 {
//                     _last_recv_time = GetUtcTimeStamp();
//                     AsyncRecvChecker();
//                     SetConnectPacket(true, 0, "");
//                     target_socket.BeginReceive(_recv_buffer,
//                         _recv_write_offset,
//                         MAX_PACKET_SIZE,
//                         SocketFlags.None,
//                         new AsyncCallback(RecvComplete),
//                         state);
//
//                     _connected = true;
//                 }
//                 else
//                 {
//                     throw new Exception("socket is not connected");
//                 }
//             }
//             catch (Exception e)
//             {
//                 // deprecated : 
//                 //int code = 0;
//                 //SocketException se = e as SocketException;
//                 //if (se != null)
//                 //    code = se.ErrorCode;
//                 SetClosePacket();
//                 Console.WriteLine("Exception(ConnectComplete) : " + e.Message);
// #if _NETWORK_TEST_
//                 Shutdown(target_socket, true, 11);
// #else
// 				Shutdown (target_socket);
// #endif
//                 target_socket = null;
//             }
//             finally
//             {
//                 _is_connecting = false;
//                 _connect_timeout_event.Set();
//             }
//         }
//
//         private void SetConnectPacket(bool connected, int code, string msg)
//         {
//             ConnectedProtocol protocol = new ConnectedProtocol();
//             protocol.code = code;
//             protocol.message = msg;
//             protocol.is_success = connected;
//
//             var contents = MessagePackSerializer.Serialize(protocol);
//             var packet = TcpNetworkHelper.MakePacket(ConnectedProtocol.PROTOCOL_CONNECTED_RES, contents);
//
//             PushRecvQueue(packet);
//         }
//
//         private void SetClosePacket()
//         {
//             DisconnectedProtocol protocol = new DisconnectedProtocol();
//             protocol.message = "disconnected";
//             
//             var contents = MessagePackSerializer.Serialize(protocol);
//             var packet = TcpNetworkHelper.MakePacket(DisconnectedProtocol.PROTOCOL_DISCONNECTED_RES, contents);
//             
//             PushRecvQueueFirst(packet);
//         }
//
//         private void RecvComplete(IAsyncResult ar)
//         {
//             // todo: add routine that not completed packet compostion
//             CustomAsyncState state = ar.AsyncState as CustomAsyncState;
//             System.Net.Sockets.Socket target_socket = state.sock;
//             if (target_socket == null)
//                 return;
//
//             if (_sock_uuid != state.uuid)
//             {
//                 target_socket.EndReceive(ar);
//                 Console.WriteLine("Debug Message(RecvComplete) : Previous socket recv complete");
// #if _NETWORK_TEST_
//                 Shutdown(target_socket, false, 12);
// #else
//                 Shutdown (target_socket, false);
// #endif                
//                 return;
//             }
//
//             try
//             {
//                 int len = target_socket.EndReceive(ar);
//                 if (len == 0)
//                 {
//                     SetClosePacket();
// #if _NETWORK_TEST_
//                     Shutdown(target_socket, true, 2);
// #else
// 					Shutdown (target_socket);
// #endif
//                 }
//                 else
//                 {
//                     int total_buffer_size = _recv_write_offset + len;
//                     int read_offset = 0;
//                     while (total_buffer_size - read_offset >= Packet.HeaderSize)
//                     {
//                         Packet packet = new Packet();
//                         packet.Size = BitConverter.ToUInt16(_recv_buffer, read_offset);
//                         packet.MsgId = BitConverter.ToUInt16(_recv_buffer, read_offset + 2);
//
//                         if (total_buffer_size - read_offset < packet.Size + Packet.HeaderSize)
//                             break;
//
//                         packet.SetData(_recv_buffer, read_offset + Packet.HeaderSize, packet.Size);
//                         PushRecvQueue(packet);
//                         read_offset += packet.Size + Packet.HeaderSize;
//                     }
//
//                     _recv_write_offset = total_buffer_size - read_offset;
//
//                     //핑 성공
//                     if (read_offset > 0)
//                     {
//                         Buffer.BlockCopy(_recv_buffer, read_offset, _recv_buffer, 0, _recv_write_offset);
//                         _client_ping = false;
//                         _last_recv_time = GetUtcTimeStamp();
//                         _recv_fail_count = 0;
//                     }
//                     else
//                     {
//                         if (++_recv_fail_count > BUFFER_FACTOR)
//                             throw new Exception("Packet recv fail.");
//                     }
//                     //byte[] newMessage = new byte[len];
//                     //Buffer.BlockCopy(_recv_buffer, 0, newMessage, 0, len);
//
//                     // while 블록이 완성되었으면 queue에 넣고
//                     // write_offset 재계산후 처리
//
//
//                     target_socket.BeginReceive(_recv_buffer,
//                         _recv_write_offset,
//                         MAX_PACKET_SIZE,
//                         SocketFlags.None,
//                         new AsyncCallback(RecvComplete),
//                         state);
//                 }
//             }
//             catch (Exception e)
//             {
//                 Console.WriteLine("Exception(RecvComplete) : " + e.Message);
//                 SetClosePacket();
// #if _NETWORK_TEST_
//                 Shutdown(target_socket, true, 4);
// #else
// 				Shutdown (target_socket);
// #endif
//             }
//         }
//
//         private void SendComplete(IAsyncResult ar)
//         {
//             CustomAsyncState state = ar.AsyncState as CustomAsyncState;
//             System.Net.Sockets.Socket target_socket = state.sock;
//             if (target_socket == null)
//                 return;
//
// #if _MISSING_ENDSEND_TEST_
//             ++_complete_send_count;
// #endif
//
//
//             if (_sock_uuid != state.uuid)
//             {
//                 target_socket.EndSend(ar);
//                 Console.WriteLine("Debug Message(SendComplete) : Previous socket send complete");
// #if _NETWORK_TEST_
//                 Shutdown(target_socket, false, 12);
// #else
//                 Shutdown (target_socket, false);
// #endif
//                 return;
//             }
//
//
//             lock (_send_lock)
//             {
//                 try
//                 {
// #if _MISSING_ENDSEND_TEST_
//                     if(_complete_send_count == 10)
// 					{
// 						Debug.LogWarning("10th stop EndSend");
// 						return;
// 					}
// #endif
//                     int len = target_socket.EndSend(ar);
//                     _send_complete_offset += len;
//
//                     if (_send_write_offset == _send_complete_offset)
//                     {
//                         _send_complete_offset = 0;
//                         _send_write_offset = 0;
//                         _is_send = false;
//                         if (_send_pending)
//                         {
//                             if (_send_que.Count <= 0)
//                             {
//                                 _send_pending = false;
//                                 //Debug.Log("SendPending Complete!");
//                             }
//                             else
//                             {
//                                 byte[] data = _send_que.Dequeue();
//                                 int size = data.Length;
//                                 if (size <= 0)
//                                 {
//                                     Console.WriteLine("Wrong packet found");
//                                 }
//                                 else
//                                 {
//                                     //Debug.Log ("send from p-que");
//                                     WriteSendBuffer(data, 0, size);
//                                 }
//                             }
//                         }
//                     }
//                     else
//                     {
//                         target_socket.BeginSend(_send_buffer, _send_complete_offset, _send_write_offset - _send_complete_offset, SocketFlags.None, new AsyncCallback(SendComplete), state);
//                         _is_send = true;
//                     }
//                 }
//                 catch (Exception e)
//                 {
//                     Console.WriteLine("Exception(SendComplete) : " + e.Message);
//                     SetClosePacket();
// #if _NETWORK_TEST_
//                     Shutdown(target_socket, true, 5);
// #else
// 					Shutdown (target_socket);
// #endif
//                 }
//             }
//         }
// #if _NETWORK_TEST_
//         private void Shutdown(System.Net.Sockets.Socket target_socket, bool is_member = true, int where = 0)
// #else
// 		private void Shutdown(System.Net.Sockets.Socket target_socket, bool is_member = true)
// #endif
//         {
//             if (target_socket != null)
//             {
//
// #if _NETWORK_TEST_
//                 Console.WriteLine("Socket Shutdowned" + where);
// #else
// 				Debug.Log ("Socket Shutdowned");
// #endif
//                 if (target_socket.Connected)
//                 {
//                     try
//                     {
//                         target_socket.Shutdown(SocketShutdown.Both);
//                     }
//                     catch (Exception e)
//                     {
//                         int code = 0;
//                         SocketException se = e as SocketException;
//                         if (se != null)
//                         {
//                             code = se.ErrorCode;
//                             Console.WriteLine("Socket shutdown exception : " + code);
//                         }
//                         else
//                         {
//                             Console.WriteLine("Socket shutdown exception : unknown");
//                         }
//                     }
//                     target_socket.Close();
//
//                 }
//                 target_socket = null;
//             }
//
//             if (is_member)
//             {
//                 _sock = null;
//                 _connected = false;
//                 _run_recv_checker = false;
//             }
//
//         }
//
//         public List<Packet> GetQueueData()
//         {
//             _tempPacketBuff.Clear();
//
//             if (_recv_que.Count <= 0)
//                 return _tempPacketBuff;
//
//             int loop_count = 0;
//             while (_recv_que.Count > 0 && loop_count < MAX_RECV_POP_COUNT)
//             {
//                 _tempPacketBuff.Add(_recv_que.Dequeue());
//                 ++loop_count;
//             }
//
//             return _tempPacketBuff;
//         }
//
//         private void WriteSendBuffer(byte[] buffer, int start_offset, int size)
//         {
//             Buffer.BlockCopy(buffer, start_offset, _send_buffer, _send_write_offset, size);
//             _send_write_offset += size;
//
//             if (_is_send)
//                 return;
//
//             try
//             {
//                 _sock.BeginSend(_send_buffer, _send_complete_offset, _send_write_offset - _send_complete_offset, SocketFlags.None, new AsyncCallback(SendComplete), _state);
//                 _is_send = true;
//             }
//             catch (Exception e)
//             {
//                 Console.WriteLine("Exception(Send1) : " + e.Message);
//                 SetClosePacket();
// #if _NETWORK_TEST_
//                 Shutdown(_sock, true, 8);
// #else
// 				Shutdown (_sock);
// #endif
//             }
//         }
//
//         public void Send(byte[] buffer, int start_offset, int end_offset)
//         {
//             int size = end_offset - start_offset;
//             if (size <= 0)
//                 return;
//
//             lock (_send_lock)
//             {
//                 //++test_count;
//                 if (_send_pending || (_send_write_offset + (end_offset - start_offset) > MAX_PACKET_SIZE))
//                 {
//                     //Debug.Log ("Send Pending" + test_count);
//                     _send_pending = true;
//                     PushSendQueue(buffer, start_offset, end_offset);
//                     return;
//                 }
//                 //Debug.Log ("Send Pending" + test_count);
//                 WriteSendBuffer(buffer, start_offset, size);
//             }
//         }
//
//         public void Send(Packet packet)
//         {
//             byte[] temp = BitConverter.GetBytes(packet.Size);
//             byte[] temp2 = BitConverter.GetBytes(packet.MsgId);
//
//             lock (_send_lock)
//             {
//                 //++test_count;
//                 int size = packet.Size + Packet.HeaderSize;
//                 if (_send_pending || (_send_write_offset + size > MAX_PACKET_SIZE))
//                 {
//                     LoggerHelper.Instance.LogInformation($"Send Pending {_send_pending}{_send_write_offset}|{size}|{_send_que.Count}|{_send_write_offset + size > MAX_PACKET_SIZE}|{_is_send}");
//                     //Debug.Log ("Send Pending" + test_count);
//                     _send_pending = true;
//                     byte[] buffer = new byte[size];
//                     Buffer.BlockCopy(temp, 0, buffer, 0, temp.Length);
//                     Buffer.BlockCopy(temp2, 0, buffer, 2, temp2.Length);
//                     Buffer.BlockCopy(packet.Contents, 0, buffer, Packet.HeaderSize, packet.Size);
//                     PushSendQueue(buffer, 0, size);
//                     return;
//                 }
//
//                 //Debug.Log ("Send " + test_count);
//
//                 Buffer.BlockCopy(temp, 0, _send_buffer, _send_write_offset, temp.Length);
//                 Buffer.BlockCopy(temp2, 0, _send_buffer, _send_write_offset + 2, temp2.Length);
//                 Buffer.BlockCopy(packet.Contents, 0, _send_buffer, _send_write_offset + Packet.HeaderSize, packet.Size);
//                 _send_write_offset += size;
//
//                 if (_is_send)
//                     return;
//
//                 try
//                 {
//                     _sock.BeginSend(_send_buffer, _send_complete_offset, _send_write_offset - _send_complete_offset, SocketFlags.None, new AsyncCallback(SendComplete), _state);
//                     _is_send = true;
//                 }
//                 catch (Exception e)
//                 {
//                     Console.WriteLine("Exception(Send2) : " + e.Message);
//                     SetClosePacket();
// #if _NETWORK_TEST_
//                     Shutdown(_sock, true, 9);
// #else
// 					Shutdown (_sock);
// #endif
//                 }
//             }
//
//         }
//
//         public bool HasData()
//         {
//             return (_recv_que.Count > 0);
//         }
//
//
//
//         void PushRecvQueue(Packet data)
//         {
//             //if (Connected () == false)
//             //	return;
//
//             _recv_que.Enqueue(data);
//         }
//
//
//         void PushRecvQueueFirst(Packet data)
//         {
//             _recv_que.EnqueueFirst(data);
//         }
//
//
//         void PushSendQueue(byte[] data, int start_offset, int end_offset)
//         {
//             byte[] newMessage = new byte[end_offset - start_offset];
//             Buffer.BlockCopy(data, start_offset, newMessage, 0, end_offset - start_offset);
//             _send_que.Enqueue(newMessage);
//         }
//
//         long GetUtcTimeStamp()
//         {
//             return DateTimeOffset.UtcNow.ToUnixTimeSeconds();
//         }
//     }
// }