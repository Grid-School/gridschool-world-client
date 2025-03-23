using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using ClientPlayerData;
using NativeWebSocket;
using Newtonsoft.Json;
using UnityEngine;

namespace Managers
{
    public class WebSocketManager
    {
        private WebSocket _websocket;
        private readonly string _serverUri;
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

        public async Task ConnectAsync() => await _websocket.Connect();

        public async void SendMessage(string message)
        {
            if (_websocket != null && _websocket.State == WebSocketState.Open)
                await _websocket.SendText(message);
        }

        private void OnMessageReceived(byte[] bytes)
        {
            string json = System.Text.Encoding.UTF8.GetString(bytes);
            Debug.Log($"Received message: {json}");
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

        public void DispatchMessageQueue() => _websocket?.DispatchMessageQueue();
        public void Close() => _websocket?.Close();
    }

    public class IdMessage
    {
        public string type { get; set; }
        public string id { get; set; }
    }
}