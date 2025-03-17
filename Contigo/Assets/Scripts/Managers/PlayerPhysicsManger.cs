using ClientPlayerData;
using Newtonsoft.Json;
using UnityEngine;

namespace Managers
{
    public class PlayerPhysicsManager
    {
        private readonly GameObject _localPlayer;
        private readonly float _speed;
        private readonly float _updateInterval;
        private readonly WebSocketManager _networkManager;
        private Vector3 _physicsPosition;
        private Vector3 _renderPosition;
        private Vector3 _collisionForce; // Accumulated force from collisions

        public PlayerPhysicsManager(GameObject player, float speed, float updateInterval, WebSocketManager networkManager)
        {
            _localPlayer = player;
            _speed = speed;
            _updateInterval = updateInterval;
            _networkManager = networkManager;
            _physicsPosition = player.transform.position;
            _renderPosition = _physicsPosition;
            _collisionForce = Vector3.zero;
        }

        public void UpdatePhysics(Snapshot snapshot, string localId)
        {
            // Reset collision force
            _collisionForce = Vector3.zero;

            // Apply collision force if present in snapshot
            if (snapshot != null && snapshot.Collisions != null && snapshot.Collisions.ContainsKey(localId))
            {
                CollisionData collision = snapshot.Collisions[localId];
                Vector3 direction = collision.ToDirection();
                float forceMagnitude = 10f; // Strong force to push back
                _collisionForce = direction * forceMagnitude * Time.fixedDeltaTime;
            }

            // Apply input movement
            Vector3 inputDir = new Vector3(Input.GetAxisRaw("Horizontal"), 0, Input.GetAxisRaw("Vertical")).normalized;
            if (inputDir != Vector3.zero)
            {
                _physicsPosition += inputDir * _speed * Time.fixedDeltaTime;
            }

            // Apply collision force
            _physicsPosition += _collisionForce;

            // Update GameObject position for sending to server
            _localPlayer.transform.position = _physicsPosition;
            SendPositionToServer();
        }

        public void InterpolateLocalPlayer()
        {
            _renderPosition = Vector3.Lerp(_renderPosition, _physicsPosition, Time.deltaTime * 10f);
            _localPlayer.transform.position = _renderPosition;
        }

        private void SendPositionToServer()
        {
            Vector3 pos = _physicsPosition;
            var msg = new InputMessage { X = pos.x, Y = pos.y, Z = pos.z };
            string json = JsonConvert.SerializeObject(msg);
            _networkManager.SendMessage(json);
        }
    }
}