using System;
using System.Collections.Generic;
using System.Linq;
using ClientPlayerData;
using UnityEngine;
using Object = UnityEngine.Object;

public class RemotePlayerManager
{
    private Dictionary<string, Managers.RemotePlayer> _remotePlayers = new Dictionary<string, Managers.RemotePlayer>();
    private Snapshot _latestSnapshot;
    private GameObject _playerPrefab;

    public RemotePlayerManager(GameObject prefab)
    {
        _playerPrefab = prefab;
    }

    public void StoreSnapshot(Snapshot snapshot, string localId)
    {
        _latestSnapshot = snapshot;

        // Cleanup
        var removeKeys = _remotePlayers.Keys.Where(id => !snapshot.Positions.ContainsKey(id)).ToList();
        foreach (var id in removeKeys)
        {
            if (Application.isEditor && !Application.isPlaying)
                Object.DestroyImmediate(_remotePlayers[id].GameObject);
            else
                Object.Destroy(_remotePlayers[id].GameObject);
            _remotePlayers.Remove(id);
        }

        // Spawn/Update
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
                _remotePlayers[kvp.Key] = new Managers.RemotePlayer(remoteObj);
            }
        }
    }

    public void InterpolateRemotePlayers()
    {
        if (_latestSnapshot == null || _remotePlayers.Count == 0) return;

        foreach (var kvp in _remotePlayers)
        {
            string id = kvp.Key;
            Managers.RemotePlayer remote = kvp.Value;
            if (_latestSnapshot.Positions.TryGetValue(id, out var posData))
            {
                Vector3 targetPos = posData.ToVector3();
                remote.GameObject.transform.position = Vector3.Lerp(
                    remote.GameObject.transform.position,
                    targetPos,
                    Time.deltaTime * 10f);
            }
        }
    }

    public void ApplyServerPositions()
    {
        if (_latestSnapshot == null || _remotePlayers.Count == 0) return;

        foreach (var kvp in _remotePlayers)
        {
            string id = kvp.Key;
            Managers.RemotePlayer remote = kvp.Value;
            if (_latestSnapshot.Positions.TryGetValue(id, out var posData))
            {
                remote.GameObject.transform.position = posData.ToVector3();
                if (_latestSnapshot.Rotations.TryGetValue(id, out var rotData))
                {
                    remote.GameObject.transform.rotation = rotData.ToQuaternion();
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