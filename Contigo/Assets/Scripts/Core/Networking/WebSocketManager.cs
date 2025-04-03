using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using NativeWebSocket;
using Newtonsoft.Json;
using UnityEngine;

namespace Core.Networking
{
    public class WebSocketManager
    {
        private WebSocket _websocket;
        private readonly string _serverUri;
        private bool _isClosing = false;

        public event Action<string> OnIdReceived;
        public event Action<Core.Data.ClientPlayerData.Snapshot> OnSnapshotReceived;

        public WebSocketManager(string serverUri) 
        {
            _serverUri = serverUri;
            _websocket = new WebSocket(_serverUri);
            _websocket.OnOpen += () => LogHelper.Log($"[WebSocketManager.cs] Connected to server at {_serverUri}");
            _websocket.OnMessage += OnMessageReceived;
            _websocket.OnClose += (code) => LogHelper.LogWarning($"[WebSocketManager.cs] WebSocket closed: {code}");
            _websocket.OnError += (error) => LogHelper.LogError($"[WebSocketManager.cs] WebSocket error: {error}");
        }

        public async Task ConnectAsync()
        {
            LogHelper.Log($"[WebSocketManager.cs] ConnectAsync called.");
            if (_websocket.State == WebSocketState.Closed)
            {
                await _websocket.Connect();
            }
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
            if (_isClosing) return;
            string json = Encoding.UTF8.GetString(bytes);

            var dict = JsonConvert.DeserializeObject<Dictionary<string, object>>(json);
            if (dict != null && dict.TryGetValue("type", out object typeObj) && typeObj.ToString() == "ID")
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
            if (_isClosing) return;
#if UNITY_EDITOR || !UNITY_WEBGL
            _websocket?.DispatchMessageQueue();
#endif
        }

        public void Close()
        {
            if (_websocket != null && !_isClosing)
            {
                _isClosing = true;
                _websocket.Close();
                _websocket = null;
                LogHelper.Log("[WebSocketManager.cs] WebSocket closed.");
            }
        }

        public class IdMessage
        {
            public string type { get; set; }
            public string id { get; set; }
        }
    }
}
