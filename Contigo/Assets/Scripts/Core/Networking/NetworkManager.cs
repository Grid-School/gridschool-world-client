using System;
using System.Threading.Tasks;
using Core.Data.ClientPlayerData;

namespace Core.Networking
{
    public class InkaNetworkManager : IDisposable
    {
        private WebSocketManager _webSocket;
        public event Action<string> OnIdReceived;
        public event Action<Snapshot> OnSnapshotReceived;

        public InkaNetworkManager(string uri)
        {
            _webSocket = new WebSocketManager(uri);
            _webSocket.OnIdReceived += id => OnIdReceived?.Invoke(id);
            _webSocket.OnSnapshotReceived += snapshot => OnSnapshotReceived?.Invoke(snapshot);
        }

        public async Task ConnectAsync() => await _webSocket.ConnectAsync();
        public void DispatchMessageQueue() => _webSocket.DispatchMessageQueue();
        public void SendMessage(string json) => _webSocket.SendMessage(json);
        public void Dispose() => _webSocket?.Close();
    }
}