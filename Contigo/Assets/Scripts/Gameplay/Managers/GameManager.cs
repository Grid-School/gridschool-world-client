using UnityEngine;
using Gameplay.Managers;

namespace Core.Networking
{
    public class GameManager : MonoBehaviour
    {
        public static GameManager Instance { get; private set; }
        public InkaNetworkManager NetworkManager => GameInitializer.NetworkManagerInstance;

        private string _serverUri;
        private GameObject _playerPrefab;
        private float _lastDispatchTime = 0f;
        private const float DispatchInterval = 0.1f; // 10 times per second

        public void Initialize(string uri, GameObject prefab)
        {
            _serverUri = uri;
            _playerPrefab = prefab;
            Debug.Log($"[GameManager] Initialized with URI: {_serverUri}");
        }

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
            Debug.Log("[GameManager] Instance created in Awake.");
        }

        private void Start()
        {
            // Validate dependencies
            if (GameInitializer.PlayerManagerInstance == null)
            {
                Debug.LogError("[GameManager] PlayerManager instance is missing!");
                return;
            }

            if (_playerPrefab == null)
            {
                Debug.LogError("[GameManager] Player prefab is not assigned!");
                return;
            }

            if (NetworkManager == null)
            {
                Debug.LogError("[GameManager] NetworkManager instance is missing!");
                return;
            }

            // Ensure RemotePlayerManager exists
            if (RemotePlayerManager.Instance == null)
            {
                Debug.Log("[GameManager] RemotePlayerManager instance not found. Adding component to GameManager GameObject.");
                gameObject.AddComponent<RemotePlayerManager>();
            }

            if (RemotePlayerManager.Instance == null)
            {
                Debug.LogError("[GameManager] Failed to create RemotePlayerManager instance!");
                return;
            }

            RemotePlayerManager.Instance.Initialize(_playerPrefab);

            // Hook up network events
            NetworkManager.OnSnapshotReceived += (snapshot) =>
            {
                string localId = GameInitializer.PlayerManagerInstance?.LocalPlayerId ?? "";
                RemotePlayerManager.Instance?.StoreSnapshot(snapshot, localId);
                Debug.Log($"[GameManager] Snapshot stored with {snapshot.Positions.Count} players at time {Time.time}.");
                // Immediately interpolate after storing the snapshot
                Debug.Log("[GameManager] Calling InterpolateRemotePlayers immediately after snapshot at time " + Time.time);
                RemotePlayerManager.Instance?.InterpolateRemotePlayers();
            };
            Debug.Log("[GameManager] Start completed.");
        }

        private void Update()
        {
            RemotePlayerManager.Instance?.InterpolateRemotePlayers();
        }

        private void FixedUpdate()
        {
            if (Time.time - _lastDispatchTime >= DispatchInterval)
            {
                Debug.Log("[GameManager] Dispatching message queue at time " + Time.time);
                NetworkManager?.DispatchMessageQueue();
                _lastDispatchTime = Time.time;
            }
            Debug.Log("[GameManager] FixedUpdate running at time " + Time.time);
        }

        private void OnDestroy()
        {
            if (NetworkManager != null)
            {
                NetworkManager.OnSnapshotReceived -= (snapshot) =>
                    RemotePlayerManager.Instance?.StoreSnapshot(snapshot, GameInitializer.PlayerManagerInstance?.LocalPlayerId ?? "");
            }
            Debug.Log("[GameManager] Destroyed and cleaned up.");
        }
    }
}