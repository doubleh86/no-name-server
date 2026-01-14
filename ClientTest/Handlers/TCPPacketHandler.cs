using ClientTest.Socket;
using ClientTest.Socket.TCPClient;
using CommonData.NetworkModels.CommonCommand;

namespace ClientTest.Handlers
{
    public abstract class TCPPacketHandler
    {
        protected ITCPClient _client;
        protected readonly Dictionary<int, Action<NetworkPackage>> _handler = new();

        public virtual void Initialized()
        {
            _RegisterHandler();
        }
        
        protected TCPPacketHandler(ITCPClient client)
        {
            _client = client;
        }

        protected virtual void _RegisterHandler()
        {
            _handler.Add(ConnectedCommand.ConnectedCommandId, OnConnected);
            _handler.Add(DisconnectedCommand.DisconnectedCommandId, OnDisconnected);
        }

        public virtual Action<NetworkPackage> GetHandler(int messageId)
        {
            return _handler.GetValueOrDefault(messageId);
        }

        protected virtual void OnConnected(NetworkPackage package)
        {
            
        }

        protected virtual void OnDisconnected(NetworkPackage package)
        {
            
        }
    }
}