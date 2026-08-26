
using UnityEngine;
using Gameplay.Managers;

namespace Core.Networking
{
    public class WebSocketPlayerController : MonoBehaviour
    {
        private InkaNetworkManager _networkManager;
        private PlayerManager _playerManager;
        private RemotePlayerManager _remotePlayerManager;
        private string _localPlayerId;

        public void Initialize(InkaNetworkManager networkManager, PlayerManager playerManager, RemotePlayerManager remotePlayerManager)
        {
            //Debug.Log("[WebSocketPlayerController] Entering Initialize on NetworkController...");
            _networkManager = networkManager;
            _playerManager = playerManager;
            _remotePlayerManager = remotePlayerManager;
            
            void HandleId(string id)
            {
                _networkManager.OnIdReceived -= HandleId;
                SpawnLocal(id);
            }

            _networkManager.OnIdReceived += HandleId;
            //Debug.Log("[WebSocketPlayerController] Initialized on NetworkController. Subscribed to OnIdReceived.");
        }
        
        private void SpawnLocal(string id)
        {
            Debug.Log($"[WebSocketPlayerController] Spawning local player {id}");
            _playerManager.SpawnLocalPlayer(id);
            _remotePlayerManager.SetLocalPlayerId(id);
        }

        private void OnIdReceived(string id)
        {
            if (string.IsNullOrEmpty(id))
            {
                Debug.LogWarning($"[WebSocketPlayerController] Received empty or null ID at time {Time.time}. Ignoring.");
                return;
            }

            Debug.Log($"[WebSocketPlayerController] OnIdReceived called with ID: {id} on NetworkController at time {Time.time}");
            Debug.Log($"[WebSocketPlayerController] PlayerManager: {(_playerManager != null ? "Assigned" : "Not Assigned")}");
            Debug.Log($"[WebSocketPlayerController] RemotePlayerManager: {(_remotePlayerManager != null ? "Assigned" : "Not Assigned")}");

            _localPlayerId = id;
            _playerManager.SpawnLocalPlayer(id);
            _remotePlayerManager.SetLocalPlayerId(id);

            Debug.Log($"[WebSocketPlayerController] Set local player ID {id} in RemotePlayerManager at time {Time.time}.");
        }

        private void OnDestroy()
        {
            if (_networkManager != null)
            {
                _networkManager.OnIdReceived -= OnIdReceived;
            }
        }
    }
}
