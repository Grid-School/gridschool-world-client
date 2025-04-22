// using System;
// using System.Text;
// using System.Threading.Tasks;
// using NativeWebSocket;
// using Newtonsoft.Json;
// using UnityEngine;
// using System.Collections.Generic;
//
// namespace Core.Networking
// {
//     public class InkaNetworkManager : IDisposable
//     {
//         private static InkaNetworkManager _instance;
//         public static InkaNetworkManager Instance => _instance;
//
//         private WebSocket _websocket;
//         private readonly string _serverUri;
//         private bool _shouldReconnect = true;
//         private int _reconnectAttempts = 0;
//         private const int MaxReconnectAttempts = 5;
//         private const int ReconnectDelayMs = 5000;
//
//         public event Action<string> OnIdReceived;
//         public event Action<Core.Data.ClientPlayerData.Snapshot> OnSnapshotReceived;
//
//         private InkaNetworkManager(string uri)
//         {
//             _serverUri = uri;
//             Debug.Log($"[InkaNetworkManager] Creating instance with URI: {uri}");
//             _websocket = new WebSocket(_serverUri);
//             _websocket.OnOpen += OnOpen;
//             _websocket.OnMessage += OnMessageReceived;
//             _websocket.OnClose += OnClose;
//             _websocket.OnError += (error) => Debug.LogError($"[InkaNetworkManager] WebSocket error: {error}");
//         }
//
//         public static InkaNetworkManager CreateInstance(string uri)
//         {
//             if (_instance == null)
//             {
//                 _instance = new InkaNetworkManager(uri);
//             }
//             else
//             {
//                 Debug.LogWarning("[InkaNetworkManager] Instance already exists.");
//             }
//             return _instance;
//         }
//
//         public async Task ConnectAsync()
//         {
//             if (_websocket == null)
//             {
//                 Debug.LogError("[InkaNetworkManager] WebSocket is null, cannot connect!");
//                 return;
//             }
//
//             Debug.Log("[InkaNetworkManager] ConnectAsync called.");
//             try
//             {
//                 await _websocket.Connect();
//             }
//             catch (Exception ex)
//             {
//                 Debug.LogError($"[InkaNetworkManager] Failed to connect to {_serverUri}: {ex.Message}");
//                 await HandleReconnect();
//             }
//         }
//
//         private void OnOpen()
//         {
//             Debug.Log($"[InkaNetworkManager] Connected to server at {_serverUri}");
//             _reconnectAttempts = 0;
//         }
//
//         public async void SendMessage(string message)
//         {
//             if (string.IsNullOrEmpty(message))
//             {
//                 Debug.LogWarning("[InkaNetworkManager] Attempted to send empty message.");
//                 return;
//             }
//
//             if (_websocket != null && _websocket.State == WebSocketState.Open)
//             {
//                 try
//                 {
//                     await _websocket.SendText(message);
//                 }
//                 catch (Exception ex)
//                 {
//                     Debug.LogError($"[InkaNetworkManager] Failed to send message: {ex.Message}");
//                     await HandleReconnect();
//                 }
//             }
//             else
//             {
//                 Debug.LogWarning("[InkaNetworkManager] WebSocket is not open, cannot send message.");
//                 await HandleReconnect();
//             }
//         }
//
//         private void OnMessageReceived(byte[] bytes)
//         {
//             if (bytes == null || bytes.Length == 0)
//             {
//                 Debug.LogWarning("[InkaNetworkManager] Received empty message.");
//                 return;
//             }
//
//             try
//             {
//                 string json = Encoding.UTF8.GetString(bytes);
//                 if (string.IsNullOrEmpty(json))
//                 {
//                     Debug.LogWarning("[InkaNetworkManager] Received empty JSON string after decoding bytes.");
//                     return;
//                 }
//
//                 Debug.Log($"[InkaNetworkManager] Raw message received: {json}");
//
//                 // Parse the message into a dictionary to check the type
//                 var dict = JsonConvert.DeserializeObject<Dictionary<string, object>>(json);
//                 if (dict == null)
//                 {
//                     Debug.LogWarning("[InkaNetworkManager] Failed to deserialize JSON into dictionary.");
//                     return;
//                 }
//
//                 if (dict.TryGetValue("type", out object typeObj) && typeObj != null && typeObj.ToString().Equals("ID", StringComparison.OrdinalIgnoreCase))
//                 {
//                     Debug.Log("[InkaNetworkManager] Processing ID message.");
//                     var idMsg = JsonConvert.DeserializeObject<IdMessage>(json);
//                     if (!string.IsNullOrEmpty(idMsg?.id))
//                     {
//                         Debug.Log($"[InkaNetworkManager] ID message deserialized: type={idMsg.type}, id={idMsg.id}");
//                         OnIdReceived?.Invoke(idMsg.id);
//                     }
//                     else
//                     {
//                         Debug.LogWarning("[InkaNetworkManager] Received invalid ID message.");
//                     }
//                 }
//                 else
//                 {
//                     Debug.Log("[InkaNetworkManager] Processing snapshot message.");
//                     var snapshot = JsonConvert.DeserializeObject<Core.Data.ClientPlayerData.Snapshot>(json);
//                     if (snapshot != null)
//                     {
//                         Debug.Log($"[InkaNetworkManager] Received snapshot with {snapshot.Positions.Count} players.");
//                         OnSnapshotReceived?.Invoke(snapshot);
//                     }
//                     else
//                     {
//                         Debug.LogWarning("[InkaNetworkManager] Received invalid snapshot.");
//                     }
//                 }
//             }
//             catch (Exception ex)
//             {
//                 Debug.LogError($"[InkaNetworkManager] Error processing message: {ex.Message}");
//             }
//         }
//
//         private async void OnClose(WebSocketCloseCode code)
//         {
//             Debug.LogWarning($"[InkaNetworkManager] WebSocket closed: {code}");
//             await HandleReconnect();
//         }
//
//         private async Task HandleReconnect()
//         {
//             if (!_shouldReconnect || _reconnectAttempts >= MaxReconnectAttempts)
//             {
//                 Debug.LogError("[InkaNetworkManager] Max reconnect attempts reached. Connection closed permanently.");
//                 _shouldReconnect = false;
//                 return;
//             }
//
//             _reconnectAttempts++;
//             Debug.Log($"[InkaNetworkManager] Attempting to reconnect ({_reconnectAttempts}/{MaxReconnectAttempts}) in {ReconnectDelayMs}ms...");
//             await Task.Delay(ReconnectDelayMs);
//
//             if (_websocket == null)
//             {
//                 _websocket = new WebSocket(_serverUri);
//                 _websocket.OnOpen += OnOpen;
//                 _websocket.OnMessage += OnMessageReceived;
//                 _websocket.OnClose += OnClose;
//                 _websocket.OnError += (error) => Debug.LogError($"[InkaNetworkManager] WebSocket error: {error}");
//             }
//
//             await ConnectAsync();
//         }
//
//         public void DispatchMessageQueue()
//         {
//             if (_websocket != null && _websocket.State == WebSocketState.Open)
//             {
//                 #if !UNITY_WEBGL || UNITY_EDITOR
//                     _websocket.DispatchMessageQueue();
//                 #else
//                     Debug.Log("[InkaNetworkManager] Skipping DispatchMessageQueue on WebGL (handled by browser).");
//                 #endif
//             }
//             else
//             {
//                 Debug.LogWarning("[InkaNetworkManager] WebSocket is not open, cannot dispatch message queue.");
//             }
//         }
//
//         public void Close()
//         {
//             _shouldReconnect = false;
//             if (_websocket != null)
//             {
//                 if (_websocket.State == WebSocketState.Open)
//                 {
//                     _websocket.Close();
//                 }
//                 _websocket = null;
//             }
//             _instance = null;
//             Debug.Log("[InkaNetworkManager] WebSocket closed.");
//         }
//
//         public void Dispose()
//         {
//             Close();
//         }
//
//         public class IdMessage
//         {
//             public string type { get; set; }
//             public string id { get; set; }
//         }
//     }
// }
using System;
using System.Collections;
using System.Collections.Generic;
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
        private bool _shouldReconnect = true;
        private int _reconnectAttempts = 0;
        private const int MaxReconnectAttempts = 5;
        private const int ReconnectDelayMs = 5000;
        private readonly Queue<byte[]> _messageQueue = new Queue<byte[]>();
        private readonly Dictionary<string, bool> _knownPlayerIds = new Dictionary<string, bool>();

        public bool IsConnected { get; private set; }
        public string LocalPlayerId { get; private set; }

        public event Action<string> OnConnected;
        public event Action<string> OnIdReceived;
        public event Action<Core.Data.ClientPlayerData.Snapshot> OnSnapshotReceived;

        private InkaNetworkManager(string uri)
        {
            _serverUri = uri;
            Debug.Log($"[InkaNetworkManager] Creating instance with URI: {uri}");
            _websocket = new WebSocket(_serverUri);
            _websocket.OnOpen += OnOpen;
            _websocket.OnMessage += OnMessageReceived;
            _websocket.OnClose += OnClose;
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
            if (_websocket == null)
            {
                Debug.LogError("[InkaNetworkManager] WebSocket is null, cannot connect!");
                return;
            }

            try
            {
                await _websocket.Connect();
            }
            catch (Exception ex)
            {
                Debug.LogError($"[InkaNetworkManager] Failed to connect to {_serverUri}: {ex.Message}");
                await HandleReconnect();
            }
        }

        private void OnOpen()
        {
            IsConnected = true;
            _reconnectAttempts = 0;
            Debug.Log($"[InkaNetworkManager] Connected to server at {_serverUri}. IsConnected: {IsConnected}");
            OnConnected?.Invoke(LocalPlayerId);
        }

        public async Task SendMessageAsync(string message)
        {
            if (string.IsNullOrEmpty(message))
            {
                return;
            }

            if (_websocket != null && _websocket.State == WebSocketState.Open)
            {
                try
                {
                    await _websocket.SendText(message);
                }
                catch (Exception ex)
                {
                    Debug.LogError($"[InkaNetworkManager] Exception while sending message: {ex.Message}");
                    await HandleReconnect();
                }
            }
            else
            {
                await HandleReconnect();
            }
        }

        private void OnMessageReceived(byte[] message)
        {
            if (message == null || message.Length == 0)
            {
                return;
            }

            lock (_messageQueue)
            {
                _messageQueue.Enqueue(message);
            }
        }

        public void DispatchMessageQueue()
        {
            lock (_messageQueue)
            {
                while (_messageQueue.Count > 0)
                {
                    byte[] message = _messageQueue.Dequeue();
                    ProcessMessage(message);
                }
            }

#if !UNITY_WEBGL || UNITY_EDITOR
            if (_websocket != null && _websocket.State == WebSocketState.Open)
            {
                _websocket.DispatchMessageQueue();
            }
#endif
        }

        private void ProcessMessage(byte[] message)
        {
            if (message == null || message.Length == 0)
            {
                return;
            }

            string messageStr = Encoding.UTF8.GetString(message);

            try
            {
                var dict = JsonConvert.DeserializeObject<Dictionary<string, object>>(messageStr);
                if (dict != null && dict.TryGetValue("type", out object typeObj) && typeObj?.ToString().Equals("ID", StringComparison.OrdinalIgnoreCase) == true)
                {
                    var idMessage = JsonConvert.DeserializeObject<IdMessage>(messageStr);
                    
                    if (string.IsNullOrEmpty(LocalPlayerId))
                    {
                        LocalPlayerId = idMessage.id;
                        OnIdReceived?.Invoke(idMessage.id);
                    }
                    else
                    {
                        Debug.Log($"[InkaNetworkManager] Ignoring extra ID message for {idMessage.id}");
                    }
                    
                    
                }
                else
                {
                    var snapshot = JsonConvert.DeserializeObject<Core.Data.ClientPlayerData.Snapshot>(messageStr);
                    if (snapshot != null)
                    {
                        OnSnapshotReceived?.Invoke(snapshot);
                    }
                    else
                    {
                        Debug.LogWarning("[InkaNetworkManager] Received invalid message: not an ID or snapshot.");
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"[InkaNetworkManager] Error processing message: {ex.Message}\nStackTrace: {ex.StackTrace}");
            }
        }

        private async void OnClose(WebSocketCloseCode code)
        {
            IsConnected = false;
            Debug.LogWarning($"[InkaNetworkManager] WebSocket closed: {code}");
            await HandleReconnect();
        }

        private async Task HandleReconnect()
        {
            if (!_shouldReconnect || _reconnectAttempts >= MaxReconnectAttempts)
            {
                Debug.LogError("[InkaNetworkManager] Max reconnect attempts reached. Connection closed permanently.");
                _shouldReconnect = false;
                return;
            }

            _reconnectAttempts++;
            Debug.Log($"[InkaNetworkManager] Attempting to reconnect ({_reconnectAttempts}/{MaxReconnectAttempts}) in {ReconnectDelayMs}ms...");
            await Task.Delay(ReconnectDelayMs);

            if (_websocket == null)
            {
                _websocket = new WebSocket(_serverUri);
                _websocket.OnOpen += OnOpen;
                _websocket.OnMessage += OnMessageReceived;
                _websocket.OnClose += OnClose;
                _websocket.OnError += (error) => Debug.LogError($"[InkaNetworkManager] WebSocket error: {error}");
            }

            await ConnectAsync();
        }

        public void Close()
        {
            _shouldReconnect = false;
            if (_websocket != null)
            {
                if (_websocket.State == WebSocketState.Open)
                {
                    _websocket.Close();
                }
                _websocket = null;
            }
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