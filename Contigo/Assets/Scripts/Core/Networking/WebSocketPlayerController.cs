using UnityEngine;
using Gameplay.Managers;
using System.Threading.Tasks;
using System;

namespace Core.Networking
{
    public class WebSocketPlayerController : MonoBehaviour
    {
        private InkaNetworkManager _networkManager;
        private bool _isDestroyed = false;

        private void Start()
        {
            Debug.Log("Starting WebSocketPlayerController");

            if (GameInitializer.NetworkManagerInstance != null)
            {
                _networkManager = GameInitializer.NetworkManagerInstance;
                _networkManager.OnIdReceived += OnIdReceived;
                Debug.Log("Using GameInitializer's NetworkManager in WebSocketPlayerController");
            }
            else
            {
                Debug.LogError("[WebSocketPlayerController] NetworkManagerInstance not found, cannot initialize network!");
            }
        }

        private void OnIdReceived(string id)
        {
            if (_isDestroyed || !gameObject.activeInHierarchy) return;
            Debug.Log($"OnIdReceived called with ID: {id} on {gameObject.name}");

            if (GameInitializer.PlayerManagerInstance == null)
            {
                Debug.LogError("[WebSocketPlayerController] PlayerManagerInstance is null! Cannot spawn player.");
                return;
            }
            GameInitializer.PlayerManagerInstance.SpawnLocalPlayer(id);
        }

        private void OnDestroy()
        {
            _isDestroyed = true;
            if (_networkManager != null)
            {
                _networkManager.OnIdReceived -= OnIdReceived;
            }
            Debug.Log($"WebSocketPlayerController destroyed: {gameObject.name}");
        }

        private void OnApplicationQuit()
        {
            if (_networkManager != null)
            {
                _networkManager.OnIdReceived -= OnIdReceived;
            }
        }
    }
}