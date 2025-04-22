// using Core.Initialization;
// using UnityEngine;
// using Gameplay.Managers;
//
// namespace Core.Networking
// {
//     public class GameManager : MonoBehaviour
//     {
//         public static GameManager Instance { get; private set; }
//         public InkaNetworkManager NetworkManager => GameInitializer.NetworkManagerInstance;
//
//         private string _serverUri;
//         private GameObject _playerPrefab;
//         private float _lastDispatchTime = 0f;
//         private const float DispatchInterval = 0.1f; // 10 times per second
//
//         public void Initialize(string uri, GameObject prefab)
//         {
//             _serverUri = uri;
//             _playerPrefab = prefab;
//             Debug.Log($"[GameManager] Initialized with URI: {_serverUri}");
//         }
//
//         private void Awake()
//         {
//             if (Instance != null && Instance != this)
//             {
//                 Destroy(gameObject);
//                 return;
//             }
//             Instance = this;
//             Debug.Log("[GameManager] Instance created in Awake.");
//         }
//
//         private void Start()
//         {
//             // Validate dependencies
//             if (GameInitializer.PlayerManagerInstance == null)
//             {
//                 Debug.LogError("[GameManager] PlayerManager instance is missing!");
//                 return;
//             }
//
//             if (_playerPrefab == null)
//             {
//                 Debug.LogError("[GameManager] Player prefab is not assigned!");
//                 return;
//             }
//
//             if (NetworkManager == null)
//             {
//                 Debug.LogError("[GameManager] NetworkManager instance is missing!");
//                 return;
//             }
//
//             // Ensure RemotePlayerManager exists
//             if (RemotePlayerManager.Instance == null)
//             {
//                 Debug.Log("[GameManager] RemotePlayerManager instance not found. Adding component to GameManager GameObject.");
//                 gameObject.AddComponent<RemotePlayerManager>();
//             }
//
//             if (RemotePlayerManager.Instance == null)
//             {
//                 Debug.LogError("[GameManager] Failed to create RemotePlayerManager instance!");
//                 return;
//             }
//
//             RemotePlayerManager.Instance.Initialize(_playerPrefab);
//
//             // Hook up network events
//             NetworkManager.OnSnapshotReceived += (snapshot) =>
//             {
//                 string localId = GameInitializer.PlayerManagerInstance?.LocalPlayerId ?? "";
//                 RemotePlayerManager.Instance?.StoreSnapshot(snapshot, localId);
//                 Debug.Log($"[GameManager] Snapshot stored with {snapshot.Positions.Count} players at time {Time.time}.");
//                 // Immediately interpolate after storing the snapshot
//                 Debug.Log("[GameManager] Calling InterpolateRemotePlayers immediately after snapshot at time " + Time.time);
//                 RemotePlayerManager.Instance?.InterpolateRemotePlayers();
//             };
//             Debug.Log("[GameManager] Start completed.");
//         }
//
//         private void Update()
//         {
//             RemotePlayerManager.Instance?.InterpolateRemotePlayers();
//         }
//
//         private void FixedUpdate()
//         {
//             if (Time.time - _lastDispatchTime >= DispatchInterval)
//             {
//                 Debug.Log("[GameManager] Dispatching message queue at time " + Time.time);
//                 NetworkManager?.DispatchMessageQueue();
//                 _lastDispatchTime = Time.time;
//             }
//             Debug.Log("[GameManager] FixedUpdate running at time " + Time.time);
//         }
//
//         private void OnDestroy()
//         {
//             if (NetworkManager != null)
//             {
//                 NetworkManager.OnSnapshotReceived -= (snapshot) =>
//                     RemotePlayerManager.Instance?.StoreSnapshot(snapshot, GameInitializer.PlayerManagerInstance?.LocalPlayerId ?? "");
//             }
//             Debug.Log("[GameManager] Destroyed and cleaned up.");
//         }
//     }
// }

using UnityEngine;
using Core.Networking;
using Gameplay.Player;
using System.Collections.Generic;
using Core.Input;

namespace Gameplay.Managers
{
    public class GameManager : MonoBehaviour
    {
        private InkaNetworkManager _networkManager;
        private PlayerManager _playerManager;
        private RemotePlayerManager _remotePlayerManager;
        private string _localClientId;

        public void Initialize(InkaNetworkManager networkManager, PlayerManager playerManager, RemotePlayerManager remotePlayerManager)
        {
            _networkManager = networkManager;
            _playerManager = playerManager;
            _remotePlayerManager = remotePlayerManager;

            _networkManager.OnConnected += OnConnected;
            _networkManager.OnSnapshotReceived += OnSnapshotReceived;
        }

        private void OnConnected(string clientId)
        {
            _localClientId = clientId;
        }

        private void OnSnapshotReceived(Core.Data.ClientPlayerData.Snapshot snapshot)
        {
            // build positions
            var positions  = new Dictionary<string, PlayerPosition>();
            foreach (var kv in snapshot.Positions)
                positions[kv.Key] = new PlayerPosition {
                    X = kv.Value.X, Y = kv.Value.Y, Z = kv.Value.Z, Angle = kv.Value.Angle
                };

            // build rotations
            var rotations = new Dictionary<string, PlayerRotation>();
            foreach (var kv in snapshot.Rotations)
                rotations[kv.Key] = new PlayerRotation {
                    X = kv.Value.X, Y = kv.Value.Y, Z = kv.Value.Z, W = kv.Value.W
                };

            // build animations
            var animations = new Dictionary<string, PlayerAnimation>();
            foreach (var kv in snapshot.Animations)
                animations[kv.Key] = new PlayerAnimation {
                    Speed       = kv.Value.Speed,
                    MotionSpeed = kv.Value.MotionSpeed,
                    Jump        = kv.Value.Jump,
                    Grounded    = kv.Value.Grounded,
                    FreeFall    = kv.Value.FreeFall
                };

            _remotePlayerManager.UpdateRemotePlayers(
                positions,
                rotations,
                null,    // velocities
                null,    // collisions
                animations
            );
        }
        
        private void Update()
        {
            _remotePlayerManager.InterpolateRemotePlayers();
        }

        private void FixedUpdate()
        {
            _networkManager.DispatchMessageQueue();
        }

        private void OnDestroy()
        {
            if (_networkManager != null)
            {
                _networkManager.OnConnected -= OnConnected;
                _networkManager.OnSnapshotReceived -= OnSnapshotReceived;
            }
        }
    }
}