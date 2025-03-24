using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Core.Data.ClientPlayerData;
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
        public event Action<Snapshot> OnSnapshotReceived;

        public WebSocketManager(string serverUri)
        {
            _serverUri = serverUri;
            _websocket = new WebSocket(_serverUri);
            _websocket.OnOpen += () => Debug.Log("Connected to server");
            _websocket.OnMessage += OnMessageReceived;
            _websocket.OnClose += (code) => Debug.Log($"WebSocket closed: {code}");
            _websocket.OnError += (error) => Debug.LogError($"WebSocket error: {error}");
        }

        public async Task ConnectAsync()
        {
            if (_websocket.State == WebSocketState.Closed) // Fixed: Removed None
            {
                await _websocket.Connect();
            }
        }

        public async void SendMessage(string message)
        {
            if (_websocket != null && _websocket.State == WebSocketState.Open)
                await _websocket.SendText(message);
        }

        private void OnMessageReceived(byte[] bytes)
        {
            if (_isClosing) return;
            string json = System.Text.Encoding.UTF8.GetString(bytes);
            Debug.Log($"Received message at {Time.time}: {json}");
            var dict = JsonConvert.DeserializeObject<Dictionary<string, object>>(json);
            if (dict != null && dict.TryGetValue("type", out object typeObj) && typeObj.ToString() == "ID")
            {
                var idMsg = JsonConvert.DeserializeObject<IdMessage>(json);
                Debug.Log($"Deserialized IdMessage: type={idMsg.type}, id={idMsg.id}");
                OnIdReceived?.Invoke(idMsg.id);
            }
            else
            {
                var snapshot = JsonConvert.DeserializeObject<Snapshot>(json);
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
            }
        }

        // Nested class for ID message deserialization
        public class IdMessage
        {
            public string type { get; set; }
            public string id { get; set; }
        }
    }
}