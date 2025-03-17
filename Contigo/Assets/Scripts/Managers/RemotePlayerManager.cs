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

            // Remove players not in snapshot
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

            // Create new remote players
            foreach (var kvp in snapshot.Positions)
            {
                string id = kvp.Key;
                if (id == localId) continue;

                if (!_remotePlayers.ContainsKey(id))
                {
                    GameObject remoteObj = GameObject.Instantiate(_playerPrefab);
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
                    _remotePlayers[id].SetPhysicsPosition(serverPos); // Set physics position
                }
            }
        }

        public void InterpolateRemotePlayers()
        {
            foreach (var player in _remotePlayers.Values)
            {
                player.Interpolate(); // Smooth rendering
            }
        }
    }

    public class RemotePlayer
    {
        public GameObject GameObject { get; private set; }
        private Vector3 _physicsPosition;
        private Vector3 _renderPosition;

        public RemotePlayer(GameObject obj)
        {
            GameObject = obj;
            _physicsPosition = obj.transform.position;
            _renderPosition = _physicsPosition;
        }

        public void SetPhysicsPosition(Vector3 position)
        {
            _physicsPosition = position;
        }

        public void Interpolate()
        {
            _renderPosition = Vector3.Lerp(_renderPosition, _physicsPosition, Time.deltaTime * 10f); // Smooth interpolation
            GameObject.transform.position = _renderPosition;
        }
    }
}