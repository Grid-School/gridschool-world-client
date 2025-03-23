using System.Collections.Generic;
using ClientPlayerData;
using Controllers;
using StarterAssets;
using UnityEngine;

namespace Managers
{
    public class RemotePlayerManager
    {
        private readonly GameObject _playerPrefab;
        private readonly Dictionary<string, RemotePlayer> _remotePlayers = new Dictionary<string, RemotePlayer>();
        private Snapshot _latestSnapshot;

        public RemotePlayerManager(GameObject prefab)
        {
            _playerPrefab = prefab;
        }

        public void StoreSnapshot(Snapshot snapshot, string localId)
        {
            _latestSnapshot = snapshot;

            // Remove players that are no longer in the snapshot
            List<string> removeKeys = new List<string>();
            foreach (var id in _remotePlayers.Keys)
            {
                if (!snapshot.Positions.ContainsKey(id))
                    removeKeys.Add(id);
            }
            foreach (var id in removeKeys)
            {
                GameObject.Destroy(_remotePlayers[id].GameObject);
                _remotePlayers.Remove(id);
                Debug.Log($"Removed remote player with ID: {id}");
            }

            // Spawn or update remote players
            foreach (var kvp in snapshot.Positions)
            {
                string id = kvp.Key;
                if (id == localId) continue; // Skip the local player

                if (!_remotePlayers.ContainsKey(id))
                {
                    // Spawn a new remote player
                    GameObject remoteObj = GameObject.Instantiate(_playerPrefab);
                    // Remove CharacterController for remote players
                    var cc = remoteObj.GetComponent<CharacterController>();
                    if (cc != null) GameObject.Destroy(cc);

                    var controller = remoteObj.GetComponent<ThirdPersonController>();
                    if (controller != null) controller.enabled = false; // Disable local control

                    // Disable Animator events for remote players
                    var animator = remoteObj.GetComponent<Animator>();
                    if (animator != null)
                    {
                        animator.applyRootMotion = false;
                        remoteObj.tag = "RemotePlayer";
                    }
                    else
                    {
                        Debug.LogWarning($"Animator not found on remote player with ID: {id}");
                    }

                    _remotePlayers[id] = new RemotePlayer(remoteObj);
                    Debug.Log($"Spawned remote player with ID: {id} at position: {remoteObj.transform.position}");
                }
            }
        }

        public void ApplyServerPositions()
        {
            if (_latestSnapshot == null) return;

            foreach (var kvp in _latestSnapshot.Positions)
            {
                string id = kvp.Key;
                if (_remotePlayers.ContainsKey(id))
                {
                    Vector3 serverPos = kvp.Value.ToVector3();
                    RemotePlayer player = _remotePlayers[id];
                    player.SetPhysicsPosition(serverPos);

                    // Use the Angle from PositionData for rotation
                    Quaternion rotation = Quaternion.Euler(0, kvp.Value.Angle, 0);
                    player.SetPhysicsRotation(rotation);

                    // Apply animation state
                    if (_latestSnapshot.Animations != null && _latestSnapshot.Animations.ContainsKey(id))
                    {
                        var animState = _latestSnapshot.Animations[id];
                        player.SetAnimationState(animState.Speed, animState.MotionSpeed, animState.Jump, animState.Grounded, animState.FreeFall);
                    }

                    Debug.Log($"Applying server position for player {id}: Position: {serverPos}, Angle: {kvp.Value.Angle}, Speed: {(_latestSnapshot.Animations != null && _latestSnapshot.Animations.ContainsKey(id) ? _latestSnapshot.Animations[id].Speed : 0)}");
                }
            }
        }

        public void InterpolateRemotePlayers()
        {
            foreach (var player in _remotePlayers.Values)
            {
                player.Interpolate();
            }
        }
    }

    public class RemotePlayer
    {
        public GameObject GameObject { get; private set; }
        private Vector3 _physicsPosition;
        private Vector3 _renderPosition;
        private Quaternion _physicsRotation;
        private Quaternion _renderRotation;
        private Animator _animator;

        // Target animation states for interpolation
        private float _targetSpeed;
        private float _targetMotionSpeed;
        private bool _targetJump;
        private bool _targetGrounded;
        private bool _targetFreeFall;

        private static readonly int SpeedHash = Animator.StringToHash("Speed");
        private static readonly int JumpHash = Animator.StringToHash("Jump");
        private static readonly int GroundedHash = Animator.StringToHash("Grounded");
        private static readonly int FreeFallHash = Animator.StringToHash("FreeFall");
        private static readonly int MotionSpeedHash = Animator.StringToHash("MotionSpeed");

        public RemotePlayer(GameObject obj)
        {
            GameObject = obj;
            _physicsPosition = obj.transform.position;
            _renderPosition = _physicsPosition;
            _physicsRotation = obj.transform.rotation;
            _renderRotation = _physicsRotation;
            _animator = obj.GetComponent<Animator>();
        }

        public Vector3 GetPhysicsPosition()
        {
            return _physicsPosition;
        }

        public void SetPhysicsPosition(Vector3 position)
        {
            _physicsPosition = position;
            GameObject.transform.position = position;
        }

        public void SetPhysicsRotation(Quaternion rotation)
        {
            _physicsRotation = rotation;
        }

        public void SetAnimationState(float speed, float motionSpeed, bool jump, bool grounded, bool freeFall)
        {
            _targetSpeed = speed;
            _targetMotionSpeed = motionSpeed;
            _targetJump = jump;
            _targetGrounded = grounded;
            _targetFreeFall = freeFall;
        }

        public void Interpolate()
        {
            float interpolationFactor = Time.deltaTime * 20f; // Faster interpolation for smoother movement
            _renderPosition = Vector3.Lerp(_renderPosition, _physicsPosition, interpolationFactor);
            _renderRotation = Quaternion.Slerp(_renderRotation, _physicsRotation, interpolationFactor);
            GameObject.transform.position = _renderPosition;
            GameObject.transform.rotation = _renderRotation;

            // Interpolate animation parameters
            if (_animator != null)
            {
                float currentSpeed = _animator.GetFloat(SpeedHash);
                float smoothedSpeed = Mathf.Lerp(currentSpeed, _targetSpeed, interpolationFactor);
                _animator.SetFloat(SpeedHash, smoothedSpeed);

                float currentMotionSpeed = _animator.GetFloat(MotionSpeedHash);
                float smoothedMotionSpeed = Mathf.Lerp(currentMotionSpeed, _targetMotionSpeed, interpolationFactor);
                _animator.SetFloat(MotionSpeedHash, smoothedMotionSpeed);

                _animator.SetBool(JumpHash, _targetJump);
                _animator.SetBool(GroundedHash, _targetGrounded);
                _animator.SetBool(FreeFallHash, _targetFreeFall);
            }
        }
    }
}