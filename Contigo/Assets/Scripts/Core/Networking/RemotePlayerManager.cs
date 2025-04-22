using UnityEngine;
using System.Collections.Generic;
using System.Linq;
using Core.Input;
using Core.Networking;
using Gameplay.Player;
using UnityEngine.InputSystem;

namespace Core.Networking
{
    public class RemotePlayerManager : MonoBehaviour
    {
        private GameObject _playerPrefab;
        private readonly Dictionary<string, GameObject> _remotePlayers = new Dictionary<string, GameObject>();
        private readonly Dictionary<string, PlayerPosition> _targetPositions = new Dictionary<string, PlayerPosition>();
        private readonly Dictionary<string, PlayerRotation> _targetRotations = new Dictionary<string, PlayerRotation>();
        private readonly Dictionary<string, PlayerAnimation> _targetAnimations = new Dictionary<string, PlayerAnimation>();
        private string _localPlayerId;
        private float _lastInterpolationTime = 0f;
        private const float InterpolationInterval = 0.1f; // 10 times per second

        private void Awake()
        {
            Debug.Log("[RemotePlayerManager] Awake completed.");
        }

        public void Initialize(GameObject playerPrefab)
        {
            _playerPrefab = playerPrefab;
            Debug.Log($"[RemotePlayerManager] Initialized with player prefab: {(_playerPrefab != null ? _playerPrefab.name : "null")}");
        }

        public void SetLocalPlayerId(string localPlayerId)
        {
            _localPlayerId = localPlayerId;
            Debug.Log($"[RemotePlayerManager] Local player ID set to: {_localPlayerId}");
        }

        public void SpawnRemotePlayer(string playerId)
        {
            // 1) Dedupe and don’t spawn your own ID
            if (_remotePlayers.ContainsKey(playerId))
            {
                Debug.Log($"[RemotePlayerManager] Player {playerId} already exists. Skipping spawn.");
                return;
            }

            if (playerId == _localPlayerId)
            {
                Debug.Log($"[RemotePlayerManager] Skipping spawn for local player ID: {playerId}");
                return;
            }

            // 2) Instantiate and book‑keep
            Debug.Log($"[RemotePlayerManager] Spawning remote player with ID: {playerId}");
            GameObject playerObj = Instantiate(_playerPrefab, Vector3.zero, Quaternion.identity);
            playerObj.name = $"RemotePlayer_{playerId}";
            playerObj.tag  = "RemotePlayer";
            _remotePlayers[playerId] = playerObj;

            // 3) Completely remove any input or local‑only logic
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

            // (Optional) Strip out any camera or UI components that could interfere
            var camera = playerObj.GetComponentInChildren<Camera>();
            if (camera != null)
            {
                Destroy(camera.gameObject); // Destroy the entire camera if it's a child
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

            Debug.Log($"[RemotePlayerManager] Remote player {playerId} fully stripped of input/local logic.");
        }


        public void UpdateRemotePlayers(
            Dictionary<string, PlayerPosition> positions,
            Dictionary<string, PlayerRotation> rotations,
            Dictionary<string, PlayerVelocity> velocities,
            Dictionary<string, PlayerCollision> collisions,
            Dictionary<string, PlayerAnimation> animations)
        {
            var toRemove = _remotePlayers.Keys.Except(positions.Keys).ToList();
            foreach (var id in toRemove)
            {
                Destroy(_remotePlayers[id]);
                _remotePlayers.Remove(id);
                _targetPositions.Remove(id);
                _targetRotations.Remove(id);
                _targetAnimations.Remove(id);
            }
            
            // Update target positions
            if (positions != null)
            {
                foreach (var kvp in positions)
                {
                    string playerId = kvp.Key;
                    if (playerId == _localPlayerId)
                    {
                        continue;
                    }

                    if (!_remotePlayers.ContainsKey(playerId))
                    {
                        SpawnRemotePlayer(playerId);
                    }
                    _targetPositions[playerId] = kvp.Value;
                }
            }

            // Update target rotations
            if (rotations != null)
            {
                foreach (var kvp in rotations)
                {
                    string playerId = kvp.Key;
                    if (playerId == _localPlayerId)
                    {
                        continue;
                    }

                    if (_remotePlayers.ContainsKey(playerId))
                    {
                        _targetRotations[playerId] = kvp.Value;
                    }
                }
            }

            // Update animations
            if (animations != null)
            {
                foreach (var kvp in animations)
                {
                    string playerId = kvp.Key;
                    if (playerId == _localPlayerId)
                    {
                        continue;
                    }

                    if (_remotePlayers.ContainsKey(playerId))
                    {
                        _targetAnimations[playerId] = kvp.Value;
                        ApplyAnimations(playerId, kvp.Value);
                    }
                }
            }
        }

        public void InterpolateRemotePlayers()
        {
            if (Time.time - _lastInterpolationTime < InterpolationInterval)
            {
                return; // Throttle interpolation
            }
            _lastInterpolationTime = Time.time;

            if (_remotePlayers.Count == 0)
            {
                return;
            }

            foreach (var kvp in _remotePlayers)
            {
                string playerId = kvp.Key;
                GameObject playerObj = kvp.Value;

                if (playerObj == null)
                {
                    Debug.LogWarning($"[RemotePlayerManager] Remote player {playerId} GameObject is null. Removing.");
                    _remotePlayers.Remove(playerId);
                    continue;
                }

                // Interpolate position
                if (_targetPositions.ContainsKey(playerId))
                {
                    var targetPos = _targetPositions[playerId];
                    Vector3 targetPosition = new Vector3(targetPos.X, targetPos.Y, targetPos.Z);
                    playerObj.transform.position = Vector3.Lerp(playerObj.transform.position, targetPosition, Time.deltaTime * 5f);
                }

                // Interpolate rotation
                if (_targetRotations.ContainsKey(playerId))
                {
                    var targetRot = _targetRotations[playerId];
                    Quaternion targetRotation = new Quaternion(targetRot.X, targetRot.Y, targetRot.Z, targetRot.W);
                    playerObj.transform.rotation = Quaternion.Slerp(playerObj.transform.rotation, targetRotation, Time.deltaTime * 5f);
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
                }
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
        }
    }
}