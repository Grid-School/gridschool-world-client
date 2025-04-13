using Gameplay.Managers;
using UnityEngine;

namespace Core.Networking
{
    public class WebSocketPlayerController : MonoBehaviour
    {
        private InkaNetworkManager _networkManager;

        private void Awake()
        {
            Debug.Log($"[WebSocketPlayerController] Awake called on {gameObject.name}. GameObject active: {gameObject.activeSelf}");
        }

        private void OnEnable()
        {
            Debug.Log($"[WebSocketPlayerController] OnEnable called on {gameObject.name}. Component enabled: {enabled}");
        }

        private void Start()
        {
            Debug.Log($"[WebSocketPlayerController] Starting on {gameObject.name}");
            _networkManager = GameInitializer.NetworkManagerInstance;
            if (_networkManager == null)
            {
                Debug.LogError("[WebSocketPlayerController] NetworkManager instance is missing!");
                return;
            }

            _networkManager.OnIdReceived += OnIdReceived;
            Debug.Log("[WebSocketPlayerController] Subscribed to NetworkManager.OnIdReceived.");
        }

        private void OnIdReceived(string id)
        {
            Debug.Log($"[WebSocketPlayerController] OnIdReceived called with ID: {id} on {gameObject.name} at time {Time.time}");
            if (GameInitializer.PlayerManagerInstance == null)
            {
                Debug.LogError("[WebSocketPlayerController] PlayerManager instance is missing!");
                return;
            }

            GameInitializer.PlayerManagerInstance.SpawnLocalPlayer(id);

            if (RemotePlayerManager.Instance != null)
            {
                RemotePlayerManager.Instance.SetLocalPlayerId(id);
                Debug.Log($"[WebSocketPlayerController] Set local player ID {id} in RemotePlayerManager at time {Time.time}.");
            }
            else
            {
                Debug.LogError("[WebSocketPlayerController] RemotePlayerManager instance is missing!");
            }
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