using System;
using UnityEngine;
using System.Collections.Generic;
using System.Linq;
using Core.Input;
using Core.Networking;
using Gameplay.Managers;
using UnityEngine.InputSystem;

namespace Core.Networking
{
    public class RemotePlayerManager : MonoBehaviour
    {
        private GameObject _playerPrefab;
        private readonly Dictionary<string, GameObject> _remotePlayers = new Dictionary<string, GameObject>();
        private readonly Dictionary<string, List<SnapshotData>> _snapshotBuffer = new Dictionary<string, List<SnapshotData>>();
        private readonly Dictionary<string, PlayerAnimation> _targetAnimations = new Dictionary<string, PlayerAnimation>();
        private string _localPlayerId;
        private const float InterpolationDelay = 0.1f;
        private const int MaxBufferSize = 5;

        private struct SnapshotData
        {
            public long Timestamp;
            public Vector3 Position;
            public Vector3 Velocity;
            public Quaternion Rotation;
            public PlayerAnimation Animation;
        }

        private void Awake()
        {
            //Debug.Log("[RemotePlayerManager] Awake completed.");
        }

        public void Initialize(GameObject playerPrefab)
        {
            _playerPrefab = playerPrefab;
            //Debug.Log($"[RemotePlayerManager] Initialized with player prefab: {(_playerPrefab != null ? _playerPrefab.name : "null")}");
        }

        public void SetLocalPlayerId(string localPlayerId)
        {
            _localPlayerId = localPlayerId;
            //Debug.Log($"[RemotePlayerManager] Local player ID set to: {_localPlayerId}");
        }

        public void SpawnRemotePlayer(string playerId)
        {
            //Debug.Log($"-------✨ [UpdateRemotePlayers] Spawning remote player: {playerId}");
            if (string.IsNullOrEmpty(playerId))
            {
                //Debug.LogWarning("[RemotePlayerManager] Attempted to spawn player with null or empty ID, skipping");
                return;
            }

            if (_remotePlayers.ContainsKey(playerId))
            {
                //Debug.Log($"[RemotePlayerManager] Player {playerId} already exists. Skipping spawn.");
                return;
            }

            if (!string.IsNullOrEmpty(_localPlayerId) && playerId == _localPlayerId)
            {
                //Debug.Log($"[RemotePlayerManager] Skipping spawn for local player ID: {playerId}");
                return;
            }

            //Debug.Log($"[RemotePlayerManager] Spawning remote player with ID: {playerId}");
            GameObject playerObj = Instantiate(_playerPrefab, Vector3.zero, Quaternion.identity);
            playerObj.name = $"RemotePlayer_{playerId}";
            playerObj.tag = "RemotePlayer";
            _remotePlayers[playerId] = playerObj;

#if ENABLE_INPUT_SYSTEM
            var playerInput = playerObj.GetComponent<PlayerInput>();
            if (playerInput != null)
            {
                Destroy(playerInput);
            }
#endif

            var setupInput = playerObj.GetComponent<SetupInputActions>();
            if (setupInput != null)
            {
                Destroy(setupInput);
            }

            var characterInput = playerObj.GetComponent<PlayerCharacterInput>();
            if (characterInput != null)
            {
                Destroy(characterInput);
            }

            var controller = playerObj.GetComponent<PlayerController>();
            if (controller != null)
            {
                Destroy(controller);
            }

            var camera = playerObj.GetComponentInChildren<Camera>();
            if (camera != null)
            {
                Destroy(camera.gameObject);
            }

            var canvas = playerObj.GetComponentInChildren<Canvas>();
            if (canvas != null)
            {
                Destroy(canvas.gameObject);
            }

            var animator = playerObj.GetComponent<Animator>();
            if (animator != null)
            {
                animator.logWarnings = false;
            }

            //Debug.Log($"[RemotePlayerManager] Remote player {playerId} fully stripped of input/local logic.");
        }

        public void StoreSnapshot(Core.Data.ClientPlayerData.Snapshot snapshot, string localPlayerId)
        {
            //Debug.Log($"-------🧩 [StoreSnapshot] Received snapshot. Positions={snapshot.Positions?.Count ?? 0} Rotations={snapshot.Rotations?.Count ?? 0} Velocities={(snapshot.Velocities != null ? snapshot.Velocities.Count : -1)} Animations={(snapshot.Animations != null ? snapshot.Animations.Count : -1)} LocalPlayerId={localPlayerId}");

            if (snapshot == null)
            {
                //Debug.LogError("[RemotePlayerManager] Received null snapshot!");
                return;
            }

            if (string.IsNullOrEmpty(_localPlayerId) && !string.IsNullOrEmpty(localPlayerId))
            {
                _localPlayerId = localPlayerId;
                //Debug.Log($"[RemotePlayerManager] Set local player ID: {_localPlayerId}");
            }

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

            if (positions.ContainsKey(_localPlayerId))
            {
                var localPos = positions[_localPlayerId];
                Vector3 serverPos = new Vector3(localPos.X, localPos.Y, localPos.Z);
                float theta = Mathf.Acos(Vector3.Dot(serverPos.normalized, Vector3.up)) * Mathf.Rad2Deg;
                //Debug.Log($"[RemotePlayerManager] Server position for local player: ({localPos.X}, {localPos.Y}, {localPos.Z}), Polar angle: {theta} degrees");
            }

            if (velocities.ContainsKey(_localPlayerId))
            {
                var localVel = velocities[_localPlayerId];
                //Debug.Log($"[RemotePlayerManager] Server velocity for local player: ({localVel.X}, {localVel.Y}, {localVel.Z})");
            }
            
            UpdateRemotePlayers(positions, rotations, velocities, collisions, animations, snapshot.Timestamp);
            
            if (snapshot.ChatMessages != null)
            {
                foreach (var kvp in snapshot.ChatMessages)
                {
                    string playerId = kvp.Key;
                    string message = kvp.Value;
                    if (_remotePlayers.TryGetValue(playerId, out GameObject playerObj))
                    {
                        ChatBubble chatBubble = playerObj.GetComponentInChildren<ChatBubble>();
                        if (chatBubble != null)
                        {
                            chatBubble.SetText(message);
                        }
                    }
                }
            }
        }

        private void UpdateRemotePlayers(
            Dictionary<string, PlayerPosition> positions,
            Dictionary<string, PlayerRotation> rotations,
            Dictionary<string, PlayerVelocity> velocities,
            Dictionary<string, PlayerCollision> collisions,
            Dictionary<string, PlayerAnimation> animations,
            long snapshotTimestamp)
        {
            //Debug.Log($"🔍------- [UpdateRemotePlayers] Starting update. Positions={positions?.Count ?? 0}, Rotations={rotations?.Count ?? 0}, Velocities={(velocities != null ? velocities.Count : -1)}");

            if (positions == null)
            {
                //Debug.LogWarning("-------[UpdateRemotePlayers] Positions dictionary is null. Skipping update.");
                return;
            }

            var toRemove = new List<string>();
            foreach (var id in _remotePlayers.Keys)
            {
                if (!positions.ContainsKey(id))
                {
                    toRemove.Add(id);
                }
            }

            foreach (var id in toRemove)
            {
                Destroy(_remotePlayers[id]);
                _remotePlayers.Remove(id);
                _snapshotBuffer.Remove(id);
                _targetAnimations.Remove(id);
                //Debug.Log($"[RemotePlayerManager] Removed disconnected player: {id}");
            }

            foreach (var kvp in positions)
            {
                string playerId = kvp.Key;
                if (string.IsNullOrEmpty(playerId) || (!string.IsNullOrEmpty(_localPlayerId) && playerId == _localPlayerId))
                {
                    //Debug.Log($"[RemotePlayerManager] Skipping update for player ID: {playerId} (localPlayerId={_localPlayerId})");
                    continue;
                }

                if (!_remotePlayers.ContainsKey(playerId))
                {
                    SpawnRemotePlayer(playerId);
                    _snapshotBuffer[playerId] = new List<SnapshotData>();
                }

                var snapshot = new SnapshotData
                {
                    Timestamp = snapshotTimestamp,
                    Position = new Vector3(kvp.Value.X, kvp.Value.Y, kvp.Value.Z),
                    Velocity = velocities != null && velocities.ContainsKey(playerId)
                        ? new Vector3(velocities[playerId].X, velocities[playerId].Y, velocities[playerId].Z)
                        : Vector3.zero,
                    Rotation = rotations != null && rotations.ContainsKey(playerId)
                        ? new Quaternion(rotations[playerId].X, rotations[playerId].Y, rotations[playerId].Z, rotations[playerId].W)
                        : Quaternion.identity,
                    Animation = animations != null && animations.ContainsKey(playerId)
                        ? animations[playerId]
                        : new PlayerAnimation()
                };

                var buffer = _snapshotBuffer[playerId];
                buffer.Add(snapshot);
                while (buffer.Count > MaxBufferSize)
                {
                    buffer.RemoveAt(0);
                }

                if (animations != null && animations.ContainsKey(playerId))
                {
                    _targetAnimations[playerId] = animations[playerId];
                    ApplyAnimations(playerId, animations[playerId]);
                }
            }
        }

        public void InterpolateRemotePlayers()
        {
            if (Time.frameCount % 2 != 0)
            {
                return;
            }

            var playersToProcess = _remotePlayers.ToList();
            foreach (var kvp in playersToProcess)
            {
                string playerId = kvp.Key;
                GameObject playerObj = kvp.Value;

                if (playerObj == null)
                {
                    //Debug.LogWarning($"[RemotePlayerManager] Remote player {playerId} GameObject is null. Removing.");
                    _remotePlayers.Remove(playerId);
                    continue;
                }

                Vector3 gravityDir = (playerObj.transform.position - PlanetManager.Instance.PlanetCenter.position).normalized;
                Quaternion targetUp = Quaternion.FromToRotation(playerObj.transform.up, gravityDir) * playerObj.transform.rotation;
                playerObj.transform.rotation = Quaternion.Slerp(playerObj.transform.rotation, targetUp, 10f * Time.deltaTime);

                if (!_snapshotBuffer.ContainsKey(playerId) || _snapshotBuffer[playerId].Count < 2)
                {
                    continue;
                }

                var buffer = _snapshotBuffer[playerId];
                long currentTime = DateTime.UtcNow.Ticks;
                long renderTime = currentTime - (long)(InterpolationDelay * TimeSpan.TicksPerSecond);

                SnapshotData? prevSnapshot = null;
                SnapshotData? nextSnapshot = null;
                for (int i = 0; i < buffer.Count - 1; i++)
                {
                    if (buffer[i].Timestamp <= renderTime && buffer[i + 1].Timestamp >= renderTime)
                    {
                        prevSnapshot = buffer[i];
                        nextSnapshot = buffer[i + 1];
                        break;
                    }
                }

                Vector3 targetPos;
                Quaternion targetRot;

                if (prevSnapshot == null || nextSnapshot == null)
                {
                    var latest = buffer[buffer.Count - 1];
                    float timeSinceSnapshot = (float)(currentTime - latest.Timestamp) / TimeSpan.TicksPerSecond;
                    targetPos = latest.Position + latest.Velocity * timeSinceSnapshot;
                    targetRot = latest.Rotation;
                }
                else
                {
                    float t = (float)(renderTime - prevSnapshot.Value.Timestamp) /
                              (nextSnapshot.Value.Timestamp - prevSnapshot.Value.Timestamp);
                    t = Mathf.Clamp01(t);

                    targetPos = Vector3.Lerp(prevSnapshot.Value.Position, nextSnapshot.Value.Position, t);

                    if (prevSnapshot.Value.Animation.Jump || !prevSnapshot.Value.Animation.Grounded)
                    {
                        float timeDelta = (float)(renderTime - prevSnapshot.Value.Timestamp) / TimeSpan.TicksPerSecond;
                        float gravity = Physics.gravity.y;
                        targetPos += gravityDir * (prevSnapshot.Value.Velocity.y * timeDelta +
                                                   0.5f * gravity * timeDelta * timeDelta);
                    }

                    targetRot = Quaternion.Slerp(prevSnapshot.Value.Rotation, nextSnapshot.Value.Rotation, t);
                }

                playerObj.transform.position = Vector3.Lerp(playerObj.transform.position, targetPos, Time.deltaTime * 10f);
                playerObj.transform.rotation = Quaternion.Slerp(playerObj.transform.rotation, targetRot, Time.deltaTime * 10f);
            }

            foreach (var buffer in _snapshotBuffer.Values)
            {
                while (buffer.Count > MaxBufferSize)
                {
                    buffer.RemoveAt(0);
                }
            }
        }

        private void ApplyAnimations(string playerId, PlayerAnimation animData)
        {
            if (_remotePlayers.TryGetValue(playerId, out GameObject playerObj))
            {
                Animator animator = playerObj.GetComponent<Animator>();
                if (animator != null)
                {
                    animator.SetFloat("Speed", animData.Speed);
                    animator.SetFloat("MotionSpeed", animData.MotionSpeed);
                    animator.SetBool("Jump", animData.Jump);
                    animator.SetBool("Grounded", animData.Grounded);
                    animator.SetBool("FreeFall", animData.FreeFall);

                    Debug.Log($"[RemotePlayerManager] Applied animations for {playerId}: Jump={animData.Jump}, Grounded={animData.Grounded}, FreeFall={animData.FreeFall}, Speed={animData.Speed}, MotionSpeed={animData.MotionSpeed}");
                }
                else
                {
                    //Debug.LogWarning($"[RemotePlayerManager] Animator not found for player {playerId}");
                }
            }
            else
            {
                //Debug.LogWarning($"[RemotePlayerManager] Player object not found for ID: {playerId}");
            }
        }

        private void OnDestroy()
        {
            foreach (var player in _remotePlayers.Values)
            {
                if (player != null)
                {
                    Destroy(player);
                }
            }
            _remotePlayers.Clear();
            _snapshotBuffer.Clear();
            _targetAnimations.Clear();
        }
    }
}