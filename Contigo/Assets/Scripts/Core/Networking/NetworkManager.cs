using System;
using System.Text;
using System.Threading.Tasks;
using NativeWebSocket;
using Newtonsoft.Json;
using UnityEngine;

namespace Core.Networking
{
    public class InkaNetworkManager : IDisposable
    {
        private static InkaNetworkManager _instance;
        public static InkaNetworkManager Instance => _instance;

        private WebSocket _websocket;
        private readonly string _serverUri;
        public event Action<string> OnIdReceived;
        public event Action<Core.Data.ClientPlayerData.Snapshot> OnSnapshotReceived;

        private InkaNetworkManager(string uri)
        {
            _serverUri = uri;
            Debug.Log($"[InkaNetworkManager] Creating instance with URI: {uri}");
            _websocket = new WebSocket(_serverUri);
            _websocket.OnOpen += () => Debug.Log($"[InkaNetworkManager] Connected to server at {_serverUri}");
            _websocket.OnMessage += OnMessageReceived;
            _websocket.OnClose += (code) => Debug.LogWarning($"[InkaNetworkManager] WebSocket closed: {code}");
            _websocket.OnError += (error) => Debug.LogError($"[InkaNetworkManager] WebSocket error: {error}");
        }

        public static InkaNetworkManager CreateInstance(string uri)
        {
            if (_instance == null)
            {
                _instance = new InkaNetworkManager(uri);
            }
            else
            {
                Debug.LogWarning("[InkaNetworkManager] Instance already exists.");
            }
            return _instance;
        }

        public async Task ConnectAsync()
        {
            Debug.Log("[InkaNetworkManager] ConnectAsync called.");
            await _websocket.Connect();
        }

        public async void SendMessage(string message)
        {
            if (_websocket != null && _websocket.State == WebSocketState.Open)
            {
                await _websocket.SendText(message);
            }
        }

        private void OnMessageReceived(byte[] bytes)
        {
            if (bytes == null || bytes.Length == 0) return;
            string json = Encoding.UTF8.GetString(bytes);
            // Check if the message is an ID message.
            if (json.Contains("\"type\":\"ID\""))
            {
                var idMsg = JsonConvert.DeserializeObject<IdMessage>(json);
                OnIdReceived?.Invoke(idMsg.id);
            }
            else
            {
                var snapshot = JsonConvert.DeserializeObject<Core.Data.ClientPlayerData.Snapshot>(json);
                if (snapshot != null)
                    OnSnapshotReceived?.Invoke(snapshot);
            }
        }

        public void DispatchMessageQueue()
        {
#if UNITY_EDITOR || !UNITY_WEBGL
            _websocket?.DispatchMessageQueue();
#endif
        }

        public void Close()
        {
            _websocket?.Close();
            _instance = null;
            Debug.Log("[InkaNetworkManager] WebSocket closed.");
        }

        public void Dispose()
        {
            Close();
        }

        public class IdMessage
        {
            public string type { get; set; }
            public string id { get; set; }
        }
    }
}
