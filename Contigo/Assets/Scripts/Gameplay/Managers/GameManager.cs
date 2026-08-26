
using UnityEngine;
using Core.Networking;
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

            // Debug.Log("[GameManager] Initialized with RemotePlayerManager: " + (_remotePlayerManager != null ? "Valid" : "Null"));
        }

        private void OnConnected(string clientId)
        {
            _localClientId = clientId;
            // Debug.Log($"[GameManager] Connected with client ID: {_localClientId}");
        }

        private void OnSnapshotReceived(Core.Data.ClientPlayerData.Snapshot snapshot)
        {
            if (snapshot == null)
            {
                //Debug.LogError("[GameManager] Received null snapshot!");
                return;
            }

            // Build positions
            var positions = new Dictionary<string, PlayerPosition>();
            if (snapshot.Positions != null)
            {
                foreach (var kv in snapshot.Positions)
                {
                    positions[kv.Key] = new PlayerPosition
                    {
                        X = kv.Value.X,
                        Y = kv.Value.Y,
                        Z = kv.Value.Z,
                        Angle = kv.Value.Angle
                    };
                }
            }
            else
            {
                //Debug.LogWarning("[GameManager] Snapshot.Positions is null");
            }

            // Build rotations
            var rotations = new Dictionary<string, PlayerRotation>();
            if (snapshot.Rotations != null)
            {
                foreach (var kv in snapshot.Rotations)
                {
                    rotations[kv.Key] = new PlayerRotation
                    {
                        X = kv.Value.X,
                        Y = kv.Value.Y,
                        Z = kv.Value.Z,
                        W = kv.Value.W
                    };
                }
            }
            else
            {
                //Debug.LogWarning("[GameManager] Snapshot.Rotations is null");
            }

            // Build velocities
            var velocities = new Dictionary<string, PlayerVelocity>();
            if (snapshot.Velocities != null)
            {
                foreach (var kv in snapshot.Velocities)
                {
                    velocities[kv.Key] = new PlayerVelocity
                    {
                        X = kv.Value.X,
                        Y = kv.Value.Y,
                        Z = kv.Value.Z
                    };
                }
            }
            else
            {
                //Debug.LogWarning("[GameManager] Snapshot.Velocities is null");
            }

            // Build collisions
            var collisions = new Dictionary<string, PlayerCollision>();
            if (snapshot.Collisions != null)
            {
                foreach (var kv in snapshot.Collisions)
                {
                    collisions[kv.Key] = new PlayerCollision
                    {
                        ColliderId = kv.Value.OtherPlayerId
                    };
                }
            }
            else
            {
                // Debug.LogWarning("[GameManager] Snapshot.Collisions is null");
            }

            // Build animations
            var animations = new Dictionary<string, PlayerAnimation>();
            if (snapshot.Animations != null)
            {
                foreach (var kv in snapshot.Animations)
                {
                    animations[kv.Key] = new PlayerAnimation
                    {
                        Speed = kv.Value.Speed,
                        MotionSpeed = kv.Value.MotionSpeed,
                        Jump = kv.Value.Jump,
                        Grounded = kv.Value.Grounded,
                        FreeFall = kv.Value.FreeFall
                    };
                }
            }
            else
            {
                // Debug.LogWarning("[GameManager] Snapshot.Animations is null");
            }

            if (_remotePlayerManager == null)
            {
                // Debug.LogError("[GameManager] RemotePlayerManager is null!");
                return;
            }

            if (_localClientId == null)
            {
                // Debug.LogWarning("[GameManager] LocalClientId is null, using empty string");
                _localClientId = "";
            }

            // Debug.Log($"[GameManager] Snapshot received with {positions.Count} positions, {rotations.Count} rotations, {velocities.Count} velocities, {collisions.Count} collisions, {animations.Count} animations");

            _remotePlayerManager.StoreSnapshot(snapshot, _localClientId);
        }

        private void Update()
        {
            if (_remotePlayerManager != null)
            {
                _remotePlayerManager.InterpolateRemotePlayers();
            }
        }

        private void FixedUpdate()
        {
            if (_networkManager != null)
            {
                _networkManager.DispatchMessageQueue();
            }
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