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
        private Dictionary<string, float> _remoteVerticalVelocities;
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
            _remoteVerticalVelocities = new Dictionary<string, float>();
            _snapshotBuffers = new Dictionary<string, SnapshotBuffer>();
        }

        public void StoreSnapshot(Snapshot snapshot, string localId)
        {
            // Remove players that are no longer in the snapshot
            var removeKeys = _remotePlayers.Keys.Where(id => !snapshot.Positions.ContainsKey(id)).ToList();
            foreach (var id in removeKeys)
            {
                if (Application.isEditor && !Application.isPlaying)
                    Object.DestroyImmediate(_remotePlayers[id].GameObject);
                else
                    Object.Destroy(_remotePlayers[id].GameObject);
                _remotePlayers.Remove(id);
                _remoteVerticalVelocities.Remove(id);
                _snapshotBuffers.Remove(id);
            }

            // Add new remote players
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
                    var controller = remoteObj.GetComponent<PlayerController>();
                    if (controller != null) controller.enabled = false;
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
                        LerpTime = 0.05f
                    };
                }
            }

            // Update snapshot buffer for existing players
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
            }
        }

        public void InterpolateRemotePlayers()
        {
            if (_remotePlayers.Count == 0) return;

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
                float z = Mathf.Lerp(currentPos.z, targetPos.z, Time.deltaTime * lerpSpeed);
                float y = currentPos.y;

                if (buffer.CurrentAnimState.Jump && !buffer.CurrentAnimState.Grounded)
                {
                    if (!_remoteVerticalVelocities.ContainsKey(id))
                        _remoteVerticalVelocities[id] = Mathf.Sqrt(1.2f * -2f * -15f);
                    y += _remoteVerticalVelocities[id] * Time.deltaTime;
                    _remoteVerticalVelocities[id] += -15f * Time.deltaTime;
                }
                else if (buffer.CurrentAnimState.Grounded)
                {
                    y = Mathf.Lerp(currentPos.y, targetPos.y, Time.deltaTime * lerpSpeed);
                    _remoteVerticalVelocities[id] = 0f;
                }

                remote.GameObject.transform.position = new Vector3(x, y, z);
                remote.GameObject.transform.rotation =
                    Quaternion.Slerp(buffer.PreviousRotation, buffer.CurrentRotation, t);

                if (animator != null)
                {
                    float speed = Mathf.Lerp(buffer.PreviousAnimState.Speed, buffer.CurrentAnimState.Speed, t);
                    float motionSpeed = Mathf.Lerp(buffer.PreviousAnimState.MotionSpeed,
                        buffer.CurrentAnimState.MotionSpeed, t);

                    // Adjust speed to ensure running animation triggers
                    // PlayerController uses MoveSpeed (2.0) and SprintSpeed (5.335)
                    // Ensure the Speed parameter reflects this for remote players
                    if (speed > 2.5f) // Threshold between walking and running
                    {
                        speed = Mathf.Clamp(speed, 0f, 5.335f); // Match SprintSpeed
                    }
                    else
                    {
                        speed = Mathf.Clamp(speed, 0f, 2.0f); // Match MoveSpeed
                    }

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
                    _remoteVerticalVelocities[id] = 0f;

                    Animator animator = remote.GameObject.GetComponent<Animator>();
                    if (animator != null)
                    {
                        float speed = buffer.CurrentAnimState.Speed;
                        if (speed > 2.5f)
                        {
                            speed = Mathf.Clamp(speed, 0f, 5.335f);
                        }
                        else
                        {
                            speed = Mathf.Clamp(speed, 0f, 2.0f);
                        }

                        animator.SetFloat("Speed", speed);
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