using System;
using System.Collections.Generic;
using System.Linq;
using Core.Data.ClientPlayerData;
using Core.Input;
using Gameplay.Player;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Core.Networking
{
    public class RemotePlayerManager
    {
        private Dictionary<string, RemotePlayer> _remotePlayers;
        private GameObject _playerPrefab;
        private Dictionary<string, SnapshotBuffer> _snapshotBuffers;

        private class SnapshotBuffer
        {
            public Vector3 PreviousPosition;
            public Quaternion PreviousRotation;
            public InkaAnimationState PreviousAnimState;
            public Vector3 CurrentPosition;
            public Quaternion CurrentRotation;
            public InkaAnimationState CurrentAnimState;
            public float Timestamp;
            public float LerpTime;
        }

        public RemotePlayerManager(GameObject prefab)
        {
            _playerPrefab = prefab;
            _remotePlayers = new Dictionary<string, RemotePlayer>();
            _snapshotBuffers = new Dictionary<string, SnapshotBuffer>();
        }

        public void StoreSnapshot(Snapshot snapshot, string localId)
        {
            Debug.Log($"[RemotePlayerManager] Storing snapshot with {snapshot.Positions.Count} players at {Time.time}");
            foreach (var kvp in snapshot.Positions)
            {
                Debug.Log($"Player {kvp.Key}: Position ({kvp.Value.X}, {kvp.Value.Y}, {kvp.Value.Z})");
            }

            var removeKeys = _remotePlayers.Keys.Where(id => !snapshot.Positions.ContainsKey(id)).ToList();
            foreach (var id in removeKeys)
            {
                if (Application.isEditor && !Application.isPlaying)
                    Object.DestroyImmediate(_remotePlayers[id].GameObject);
                else
                    Object.Destroy(_remotePlayers[id].GameObject);
                _remotePlayers.Remove(id);
                _snapshotBuffers.Remove(id);
                Debug.Log($"[RemotePlayerManager] Removed player {id}");
            }

            foreach (var kvp in snapshot.Positions)
            {
                if (kvp.Key == localId) continue;
                if (!_remotePlayers.ContainsKey(kvp.Key))
                {
                    var remoteObj = Object.Instantiate(_playerPrefab);
#if UNITY_EDITOR
                    remoteObj.tag = TagExists("RemotePlayer") ? "RemotePlayer" : "Untagged";
#else
                    remoteObj.tag = "RemotePlayer";
#endif
                    remoteObj.layer = LayerMask.NameToLayer("RemotePlayer");
                    var controller = remoteObj.GetComponent<PlayerController>();
                    if (controller != null)
                    {
                        controller.enabled = false;
                        Debug.Log($"[RemotePlayerManager] Disabled PlayerController for {kvp.Key}");
                    }
                    var charController = remoteObj.GetComponent<CharacterController>();
                    if (charController != null)
                    {
                        Debug.Log($"[RemotePlayerManager] CharacterController kept enabled for {kvp.Key}");
                    }
                    var inputs = remoteObj.GetComponent<PlayerCharacterInput>();
                    if (inputs != null) inputs.enabled = false;
                    else Debug.LogWarning($"PlayerCharacterInput not found on remote player {kvp.Key}.");
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
                        LerpTime = 0.1f // Matches server 20 Hz (0.05s) roughly
                    };
                    Debug.Log($"[RemotePlayerManager] Added new player {kvp.Key} at {kvp.Value.ToVector3()}");
                }
            }

            foreach (var kvp in snapshot.Positions)
            {
                if (kvp.Key == localId || !_snapshotBuffers.ContainsKey(kvp.Key)) continue;

                var buffer = _snapshotBuffers[kvp.Key];
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
                Debug.Log($"[RemotePlayerManager] Updated buffer for {kvp.Key}: New Pos={buffer.CurrentPosition}");
            }
        }

        public void InterpolateRemotePlayers()
        {
            if (_remotePlayers.Count == 0) return;
            Debug.Log($"[RemotePlayerManager] Interpolating {_remotePlayers.Count} players at {Time.time}");

            foreach (var kvp in _remotePlayers)
            {
                string id = kvp.Key;
                RemotePlayer remote = kvp.Value;
                Animator animator = remote.GameObject.GetComponent<Animator>();
                if (!_snapshotBuffers.TryGetValue(id, out var buffer)) continue;

                float t = Mathf.Clamp01((Time.time - buffer.Timestamp) / buffer.LerpTime);
                float lerpSpeed = 20f;

                Vector3 currentPos = remote.GameObject.transform.position;
                Vector3 targetPos = Vector3.Lerp(buffer.PreviousPosition, buffer.CurrentPosition, t);
                float x = Mathf.Lerp(currentPos.x, targetPos.x, Time.deltaTime * lerpSpeed);
                float y = Mathf.Lerp(currentPos.y, targetPos.y, Time.deltaTime * lerpSpeed); // Use snapshot Y directly
                float z = Mathf.Lerp(currentPos.z, targetPos.z, Time.deltaTime * lerpSpeed);

                Vector3 newPosition = new Vector3(x, y, z);
                remote.GameObject.transform.position = newPosition;
                remote.GameObject.transform.rotation = Quaternion.Slerp(buffer.PreviousRotation, buffer.CurrentRotation, t);

                Debug.Log($"Player {id}: t={t}, Timestamp={buffer.Timestamp}, PrevPos={buffer.PreviousPosition}, CurrPos={buffer.CurrentPosition}, NewPos={newPosition}, Jump={buffer.CurrentAnimState.Jump}, Grounded={buffer.CurrentAnimState.Grounded}, FreeFall={buffer.CurrentAnimState.FreeFall}");

                if (animator != null)
                {
                    float speed = Mathf.Lerp(buffer.PreviousAnimState.Speed, buffer.CurrentAnimState.Speed, t);
                    float motionSpeed = Mathf.Lerp(buffer.PreviousAnimState.MotionSpeed, buffer.CurrentAnimState.MotionSpeed, t);
                    animator.SetFloat("Speed", speed);
                    animator.SetFloat("MotionSpeed", motionSpeed);
                    animator.SetBool("Jump", buffer.CurrentAnimState.Jump);
                    animator.SetBool("Grounded", buffer.CurrentAnimState.Grounded);
                    animator.SetBool("FreeFall", buffer.CurrentAnimState.FreeFall);
                }
            }
        }

        public void ApplyServerPositions()
        {
            if (_remotePlayers.Count == 0) return;

            foreach (var kvp in _remotePlayers)
            {
                string id = kvp.Key;
                RemotePlayer remote = kvp.Value;
                if (_snapshotBuffers.TryGetValue(id, out var buffer))
                {
                    remote.GameObject.transform.position = buffer.CurrentPosition;
                    remote.GameObject.transform.rotation = buffer.CurrentRotation;

                    Animator animator = remote.GameObject.GetComponent<Animator>();
                    if (animator != null)
                    {
                        animator.SetFloat("Speed", buffer.CurrentAnimState.Speed);
                        animator.SetFloat("MotionSpeed", buffer.CurrentAnimState.MotionSpeed);
                        animator.SetBool("Jump", buffer.CurrentAnimState.Jump);
                        animator.SetBool("Grounded", buffer.CurrentAnimState.Grounded);
                        animator.SetBool("FreeFall", buffer.CurrentAnimState.FreeFall);
                    }
                }
            }
        }

#if UNITY_EDITOR
        private bool TagExists(string tag)
        {
            return Array.Exists(UnityEditorInternal.InternalEditorUtility.tags, t => t == tag);
        }
#endif
    }
}