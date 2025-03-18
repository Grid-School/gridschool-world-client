using System.Collections.Generic;
using ClientPlayerData;
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
            }

            foreach (var kvp in snapshot.Positions)
            {
                string id = kvp.Key;
                if (id == localId) continue;

                if (!_remotePlayers.ContainsKey(id))
                {
                    GameObject remoteObj = GameObject.Instantiate(_playerPrefab);
                    if (!remoteObj.GetComponent<CharacterController>())
                    {
                        var cc = remoteObj.AddComponent<CharacterController>();
                        cc.radius = 0.5f;
                        cc.height = 2f;
                    }
                    _remotePlayers[id] = new RemotePlayer(remoteObj);
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
                    Vector3 oldPos = player.GetPhysicsPosition();
                    player.SetPhysicsPosition(serverPos);

                    if (_latestSnapshot.Rotations != null && _latestSnapshot.Rotations.ContainsKey(id))
                    {
                        Quaternion rotation = _latestSnapshot.Rotations[id].ToQuaternion();
                        player.SetPhysicsRotation(rotation);
                    }
                    else
                    {
                        Vector3 moveDir = serverPos - oldPos;
                        if (moveDir.sqrMagnitude > 0.01f)
                        {
                            Quaternion rotation = Quaternion.LookRotation(moveDir.normalized, Vector3.up);
                            player.SetPhysicsRotation(rotation);
                        }
                    }
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

        public RemotePlayer(GameObject obj)
        {
            GameObject = obj;
            _physicsPosition = obj.transform.position;
            _renderPosition = _physicsPosition;
            _physicsRotation = obj.transform.rotation;
            _renderRotation = _physicsRotation;
        }

        public Vector3 GetPhysicsPosition()
        {
            return _physicsPosition;
        }

        public void SetPhysicsPosition(Vector3 position)
        {
            _physicsPosition = position;
            CharacterController cc = GameObject.GetComponent<CharacterController>();
            if (cc != null)
            {
                cc.enabled = false;
                GameObject.transform.position = position;
                cc.enabled = true;
            }
        }

        public void SetPhysicsRotation(Quaternion rotation)
        {
            _physicsRotation = rotation;
        }

        public void Interpolate()
        {
            _renderPosition = Vector3.Lerp(_renderPosition, _physicsPosition, Time.deltaTime * 10f);
            _renderRotation = Quaternion.Slerp(_renderRotation, _physicsRotation, Time.deltaTime * 10f);
            GameObject.transform.position = _renderPosition;
            GameObject.transform.rotation = _renderRotation;
        }
    }
}