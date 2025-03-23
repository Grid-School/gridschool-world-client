using ClientPlayerData;
using Managers;
using UnityEngine;

public class PlayerManager
{
    private GameObject _playerPrefab;
    private GameObject _localPlayer;
    private string _localId;
    private RemotePlayerManager _remoteManager;

    public PlayerManager(GameObject prefab)
    {
        _playerPrefab = prefab;
        _remoteManager = new RemotePlayerManager(prefab);
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
        _remoteManager.StoreSnapshot(snapshot, _localId);
    }

    public void InterpolateRemotePlayers() => _remoteManager.InterpolateRemotePlayers();

    public GameObject LocalPlayer => _localPlayer;
}