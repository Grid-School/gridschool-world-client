using ClientPlayerData;
using Newtonsoft.Json;
using UnityEngine;

namespace Managers
{
    public class PlayerPhysicsManager
    {
        private readonly GameObject _localPlayer;
        private readonly CharacterController _controller;
        private readonly float _speed;
        private readonly float _rotationSpeed;
        private readonly float _updateInterval;
        private readonly WebSocketManager _networkManager;
        private Vector3 _physicsPosition;
        private Vector3 _renderPosition;
        private Quaternion _physicsRotation;
        private Quaternion _renderRotation;
        private float _angle;
        private float _verticalVelocity;
        private readonly float _gravity = -34f; // m/s²
        private readonly float _jumpSpeed = 15f; // m/s, upward velocity for jump
        private bool _jumpRequested; // Buffer jump input

        public PlayerPhysicsManager(GameObject player, float speed, float rotationSpeed, float updateInterval, WebSocketManager networkManager)
        {
            _localPlayer = player;
            _controller = player.GetComponent<CharacterController>();
            if (_controller == null)
            {
                Debug.LogError("Player prefab is missing a CharacterController component!");
                _controller = player.AddComponent<CharacterController>();
                _controller.radius = 0.5f;
                _controller.height = 2f;
                _controller.center = new Vector3(0, 1f, 0);
            }
            _speed = speed;
            _rotationSpeed = rotationSpeed;
            _updateInterval = updateInterval;
            _networkManager = networkManager;
            _physicsPosition = player.transform.position;
            _renderPosition = _physicsPosition;
            _angle = 0f;
            _physicsRotation = Quaternion.Euler(0, _angle, 0);
            _renderRotation = _physicsRotation;
            _verticalVelocity = 0f;
            _jumpRequested = false;
        }

        public void UpdatePhysics(Snapshot snapshot, string localId)
        {
            Vector3 moveDir = Vector3.zero;

            // Rotation
            float horizontalInput = Input.GetAxisRaw("Horizontal");
            if (horizontalInput != 0)
            {
                _angle += horizontalInput * _rotationSpeed * Time.fixedDeltaTime;
                _physicsRotation = Quaternion.Euler(0, _angle, 0);
            }

            // Horizontal movement
            float verticalInput = Input.GetAxisRaw("Vertical");
            if (verticalInput != 0)
            {
                Vector3 direction = _physicsRotation * Vector3.forward;
                moveDir = direction * verticalInput * _speed;
            }

            // Check grounding first
            if (_controller.isGrounded && _verticalVelocity < 0)
            {
                _verticalVelocity = -0.1f; // Reset velocity early
            }

            // Apply jump if requested and grounded
            if (_jumpRequested && _controller.isGrounded)
            {
                _verticalVelocity = _jumpSpeed;
                _jumpRequested = false; // Clear request
            }

            // Apply gravity
            _verticalVelocity += _gravity * Time.fixedDeltaTime;
            moveDir.y = _verticalVelocity;

            // Move
            _controller.Move(moveDir * Time.fixedDeltaTime);
            _physicsPosition = _localPlayer.transform.position;

            SendPositionToServer();
        }

        public void InterpolateLocalPlayer()
        {
            // Capture jump input in Update for better responsiveness
            if (Input.GetKeyDown(KeyCode.Space))
            {
                _jumpRequested = true;
            }

            _renderPosition = Vector3.Lerp(_renderPosition, _physicsPosition, Time.deltaTime * 20f);
            _renderRotation = Quaternion.Slerp(_renderRotation, _physicsRotation, Time.deltaTime * 20f);
            _localPlayer.transform.position = _renderPosition;
            _localPlayer.transform.rotation = _renderRotation;
        }

        private void SendPositionToServer()
        {
            Vector3 pos = _physicsPosition;
            var msg = new InputMessage { X = pos.x, Y = pos.y, Z = pos.z, Angle = _angle };
            string json = JsonConvert.SerializeObject(msg);
            _networkManager.SendMessage(json);
        }
    }
}