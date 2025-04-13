using System;
using System.Collections.Generic;
using System.Linq;
using Core.Data.ClientPlayerData;
using Core.Input;
using Gameplay.Player;
using UnityEngine;

namespace Core.Networking
{
public class RemotePlayerManager : MonoBehaviour
{
    private Dictionary<string, RemotePlayer> _remotePlayers;
    private Dictionary<string, SnapshotBuffer> _snapshotBuffers;
    private GameObject _playerPrefab;
    private string _localPlayerId;

    private class SnapshotBuffer
    {
        public Vector3 PreviousPosition;
        public Quaternion PreviousRotation;
        public InkaAnimationState PreviousAnimState;
        public Vector3 CurrentPosition;
        public Quaternion CurrentRotation;
        public InkaAnimationState CurrentAnimState;
        public float Timestamp;
        public float SnapshotInterval; // Average interval between snapshots
        public List<float> SnapshotIntervalHistory; // For smoothing interval
        public const int HistorySize = 3;
    }

    public static RemotePlayerManager Instance { get; private set; }

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(this);
            Debug.LogWarning("[RemotePlayerManager] Another instance already exists. Destroying this one.");
            return;
        }
        Instance = this;

        _remotePlayers = new Dictionary<string, RemotePlayer>();
        _snapshotBuffers = new Dictionary<string, SnapshotBuffer>();
        Debug.Log("[RemotePlayerManager] Awake completed.");
    }

    public void Initialize(GameObject prefab)
    {
        _playerPrefab = prefab;
        if (_playerPrefab == null)
        {
            Debug.LogError("[RemotePlayerManager] Player prefab is null!");
        }
        else
        {
            Debug.Log("[RemotePlayerManager] Initialized with player prefab: " + _playerPrefab.name);
        }
    }

    public void SetLocalPlayerId(string id)
    {
        _localPlayerId = id;
        Debug.Log($"[RemotePlayerManager] Local player ID set to {_localPlayerId}");
    }

    public void StoreSnapshot(Snapshot snapshot, string localId)
    {
        if (snapshot == null || snapshot.Positions == null)
        {
            Debug.LogWarning("[RemotePlayerManager] Received null snapshot or positions.");
            return;
        }

        Debug.Log($"[RemotePlayerManager] Storing snapshot with {snapshot.Positions.Count} players at {Time.time}");
        foreach (var kvp in snapshot.Positions)
        {
            Debug.Log($"[RemotePlayerManager] Player {kvp.Key}: Position ({kvp.Value.X}, {kvp.Value.Y}, {kvp.Value.Z})");
        }

        var removeKeys = _remotePlayers.Keys.Where(id => !snapshot.Positions.ContainsKey(id)).ToList();
        foreach (var id in removeKeys)
        {
            if (_remotePlayers[id]?.GameObject != null)
            {
                if (Application.isEditor && !Application.isPlaying)
                    UnityEngine.Object.DestroyImmediate(_remotePlayers[id].GameObject);
                else
                    UnityEngine.Object.Destroy(_remotePlayers[id].GameObject);
            }
            _remotePlayers.Remove(id);
            _snapshotBuffers.Remove(id);
            Debug.Log($"[RemotePlayerManager] Removed player {id}");
        }

        foreach (var kvp in snapshot.Positions)
        {
            if (string.IsNullOrEmpty(kvp.Key) || kvp.Key == _localPlayerId)
            {
                Debug.Log($"[RemotePlayerManager] Skipping player {kvp.Key} (Local player ID: {_localPlayerId})");
                continue;
            }

            if (!_remotePlayers.ContainsKey(kvp.Key))
            {
                if (_playerPrefab == null)
                {
                    Debug.LogError("[RemotePlayerManager] Cannot instantiate player: prefab is null!");
                    continue;
                }

                var remoteObj = UnityEngine.Object.Instantiate(_playerPrefab);

                var controller = remoteObj.GetComponent<PlayerController>();
                if (controller != null) controller.enabled = false;

                var inputs = remoteObj.GetComponent<PlayerCharacterInput>();
                if (inputs != null) inputs.enabled = false;
                else Debug.LogWarning($"[RemotePlayerManager] PlayerCharacterInput not found on remote player {kvp.Key}.");

                _remotePlayers[kvp.Key] = new RemotePlayer(remoteObj);
                _snapshotBuffers[kvp.Key] = new SnapshotBuffer
                {
                    PreviousPosition = kvp.Value.ToVector3(),
                    CurrentPosition = kvp.Value.ToVector3(),
                    PreviousRotation = Quaternion.identity,
                    CurrentRotation = Quaternion.identity,
                    PreviousAnimState = new InkaAnimationState(),
                    CurrentAnimState = new InkaAnimationState(),
                    Timestamp = Time.time,
                    SnapshotInterval = 0.1f, // Default for 10 FPS
                    SnapshotIntervalHistory = new List<float>()
                };
                Debug.Log($"[RemotePlayerManager] Added new player {kvp.Key} at {kvp.Value.ToVector3()}");
            }
        }

        foreach (var kvp in snapshot.Positions)
        {
            if (kvp.Key == _localPlayerId || !_snapshotBuffers.ContainsKey(kvp.Key)) continue;

            var buffer = _snapshotBuffers[kvp.Key];
            float snapshotInterval = Time.time - buffer.Timestamp;

            buffer.SnapshotIntervalHistory.Add(snapshotInterval);
            if (buffer.SnapshotIntervalHistory.Count > SnapshotBuffer.HistorySize)
                buffer.SnapshotIntervalHistory.RemoveAt(0);
            buffer.SnapshotInterval = buffer.SnapshotIntervalHistory.Average();

            buffer.PreviousPosition = buffer.CurrentPosition;
            buffer.CurrentPosition = kvp.Value.ToVector3();
            buffer.PreviousRotation = buffer.CurrentRotation;
            buffer.CurrentRotation = snapshot.Rotations.TryGetValue(kvp.Key, out var rotData)
                ? rotData.ToQuaternion()
                : buffer.CurrentRotation;
            buffer.PreviousAnimState = buffer.CurrentAnimState;
            buffer.CurrentAnimState = snapshot.Animations.TryGetValue(kvp.Key, out var animState)
                ? animState
                : buffer.CurrentAnimState;
            buffer.Timestamp = Time.time;

            if (_remotePlayers.TryGetValue(kvp.Key, out var remote) && remote.GameObject != null)
            {
                Vector3 currentPos = remote.GameObject.transform.position;
                buffer.PreviousPosition = Vector3.Lerp(currentPos, buffer.PreviousPosition, 0.5f);
            }

            Debug.Log($"[RemotePlayerManager] Updated buffer for {kvp.Key}: New Pos={buffer.CurrentPosition}, SnapshotInterval={buffer.SnapshotInterval}");
        }
    }

    public void InterpolateRemotePlayers()
    {
        if (string.IsNullOrEmpty(_localPlayerId))
        {
            Debug.Log("[RemotePlayerManager] No local player ID set yet. Cannot interpolate remote players.");
            return;
        }

        if (_remotePlayers.Count == 0)
        {
            Debug.Log("[RemotePlayerManager] No remote players to interpolate. RemotePlayers count: " + _remotePlayers.Count);
            return;
        }

        var playerIds = new List<string>(_remotePlayers.Keys);
        foreach (var id in playerIds)
        {
            if (!_remotePlayers.TryGetValue(id, out var remote))
            {
                Debug.LogWarning($"[RemotePlayerManager] Player {id} not found in _remotePlayers during iteration. Skipping.");
                continue;
            }

            if (remote?.GameObject == null)
            {
                Debug.LogWarning($"[RemotePlayerManager] Remote player {id} has null GameObject. Skipping.");
                continue;
            }

            if (!_snapshotBuffers.TryGetValue(id, out var buffer))
            {
                Debug.LogWarning($"[RemotePlayerManager] No snapshot buffer found for player {id}. Skipping.");
                continue;
            }

            // Calculate interpolation factor based on the smoothed snapshot interval
            float t = (Time.time - buffer.Timestamp) / buffer.SnapshotInterval;
            t = Mathf.Clamp01(t); // Always interpolate between Previous and Current

            // Interpolate position directly
            Vector3 targetPos = Vector3.Lerp(buffer.PreviousPosition, buffer.CurrentPosition, t);

            // Smooth the position update
            Vector3 currentPos = remote.GameObject.transform.position;
            float smoothFactor = Mathf.Clamp01(Time.deltaTime / buffer.SnapshotInterval); // Frequency-aware smoothing
            float x = Mathf.Lerp(currentPos.x, targetPos.x, smoothFactor);
            float y = Mathf.Lerp(currentPos.y, targetPos.y, smoothFactor);
            float z = Mathf.Lerp(currentPos.z, targetPos.z, smoothFactor);

            // Snap Y to ensure height accuracy at the end of interpolation
            if (t >= 0.99f)
            {
                y = buffer.CurrentPosition.y;
            }

            Vector3 newPosition = new Vector3(x, y, z);
            remote.GameObject.transform.position = newPosition;
            remote.GameObject.transform.rotation = Quaternion.Slerp(buffer.PreviousRotation, buffer.CurrentRotation, t);

            Animator animator = remote.GameObject.GetComponent<Animator>();
            if (animator != null)
            {
                float speed = Mathf.Lerp(buffer.PreviousAnimState.Speed, buffer.CurrentAnimState.Speed, t);
                float motionSpeed = Mathf.Lerp(buffer.PreviousAnimState.MotionSpeed, buffer.CurrentAnimState.MotionSpeed, t);
                animator.SetFloat("Speed", speed);
                animator.SetFloat("MotionSpeed", motionSpeed);
                animator.SetBool("Jump", buffer.CurrentAnimState.Jump);
                animator.SetBool("Grounded", buffer.CurrentAnimState.Grounded);
                animator.SetBool("FreeFall", buffer.CurrentAnimState.FreeFall);
                Debug.Log($"[RemotePlayerManager] Updated animator for player {id}: Speed={speed}, MotionSpeed={motionSpeed}");
            }
        }
    }

    private void OnDestroy()
    {
        foreach (var player in _remotePlayers.Values)
        {
            if (player?.GameObject != null)
            {
                if (Application.isEditor && !Application.isPlaying)
                    UnityEngine.Object.DestroyImmediate(player.GameObject);
                else
                    UnityEngine.Object.Destroy(player.GameObject);
            }
        }
        _remotePlayers.Clear();
        _snapshotBuffers.Clear();
        Debug.Log("[RemotePlayerManager] Destroyed and cleaned up.");
    }
}
}