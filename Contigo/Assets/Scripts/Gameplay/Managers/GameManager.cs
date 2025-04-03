using System.Threading.Tasks;
using UnityEngine;
using Gameplay.Managers;
using Core.Networking;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }
    public InkaNetworkManager NetworkManager => GameInitializer.NetworkManagerInstance;
    public RemotePlayerManager RemotePlayers { get; private set; }

    private string serverUri;
    private GameObject playerPrefab;

    public void Initialize(string uri, GameObject prefab)
    {
        serverUri = uri;
        playerPrefab = prefab;
    }

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        Debug.Log("[GameManager] Instance created in Awake.");
    }

    private void Start()
    {
        if (GameInitializer.PlayerManagerInstance == null)
        {
            Debug.LogError("[GameManager] PlayerManager instance is missing!");
            return;
        }

        if (playerPrefab == null)
        {
            Debug.LogError("[GameManager] Player prefab is not assigned!");
            return;
        }

        if (NetworkManager == null)
        {
            Debug.LogError("[GameManager] NetworkManager instance is missing!");
            return;
        }

        // Hook up network events (only for snapshots)
        RemotePlayers = new RemotePlayerManager(playerPrefab);
        NetworkManager.OnSnapshotReceived += (snapshot) =>
        {
            if (GameInitializer.PlayerManagerInstance.LocalPlayer != null)
            {
                string localId = GameInitializer.PlayerManagerInstance.LocalPlayerId;
                RemotePlayers.StoreSnapshot(snapshot, localId);
                Debug.Log($"[GameManager] Snapshot stored with {snapshot.Positions.Count} players.");
            }
        };
    }

    private float lastDispatchTime = 0;
    private float dispatchInterval = 0.1f; // 10 times per second

    private void Update()
    {
        if (Time.time - lastDispatchTime >= dispatchInterval)
        {
            NetworkManager?.DispatchMessageQueue();
            lastDispatchTime = Time.time;
        }
        RemotePlayers?.InterpolateRemotePlayers();
    }

    private void OnDestroy()
    {
        if (NetworkManager != null)
        {
            NetworkManager.OnSnapshotReceived -= (snapshot) =>
                RemotePlayers?.StoreSnapshot(snapshot, GameInitializer.PlayerManagerInstance?.LocalPlayerId ?? "");
        }
        Debug.Log("[GameManager] Destroyed.");
    }
}