
using Core.Data.ClientPlayerData;
using Core.Networking;
using UnityEngine;

namespace Gameplay.Managers
{
    public class PlayerManager
    {
        private GameObject _playerPrefab;
        private GameObject _localPlayer;
        private string _localId;
        public RemotePlayerManager RemoteManager { get; private set; } // Public property

        public PlayerManager(GameObject prefab)
        {
            _playerPrefab = prefab;
            RemoteManager = new RemotePlayerManager(prefab);
        }

        public void SpawnLocalPlayer(string id)
        {
            _localId = id;
            if (_localPlayer == null)
            {
                _localPlayer = Object.Instantiate(_playerPrefab, new Vector3(0, 1, 0), Quaternion.identity);
                Debug.Log($"Local player spawned with ID: {id}");
            }
        }

        public void UpdateRemotePlayers(Snapshot snapshot)
        {
            if (_localPlayer != null && snapshot.Positions.ContainsKey(_localId))
            {
                Vector3 pos = snapshot.Positions[_localId].ToVector3();
                _localPlayer.transform.position = pos;
            }

            RemoteManager.StoreSnapshot(snapshot, _localId); // Fixed: Use RemoteManager
        }

        public GameObject LocalPlayer => _localPlayer;
    }
}