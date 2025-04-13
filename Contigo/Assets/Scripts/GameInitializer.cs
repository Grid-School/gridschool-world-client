using UnityEngine;
using System.Collections;
using Core.Networking;
using Gameplay.Managers;
using InkaCamera;

public class GameInitializer : MonoBehaviour
{
    public static InkaNetworkManager NetworkManagerInstance { get; private set; }
    public static PlayerManager PlayerManagerInstance { get; private set; }
    public static GameManager GameManagerInstance { get; private set; }

    [SerializeField] private string serverUri = "wss://api.inkaverse.co/ws";
    [SerializeField] private GameObject playerPrefab;
    [SerializeField] private Camera mainCamera;

    private void Awake()
    {
        StartCoroutine(InitializeGame());
    }

    private IEnumerator InitializeGame()
    {
        // Initialize PlayerManager with the player prefab
        PlayerManagerInstance = gameObject.AddComponent<PlayerManager>();
        PlayerManagerInstance.Initialize(playerPrefab);
        Debug.Log("[GameInitializer] PlayerManager instance created.");

        // Initialize GameManager
        GameManagerInstance = gameObject.AddComponent<GameManager>();
        GameManagerInstance.Initialize(serverUri, playerPrefab);
        Debug.Log("[GameInitializer] GameManager instance created.");

        // Setup camera
        if (mainCamera != null)
        {
            CameraController cameraController = mainCamera.GetComponent<CameraController>();
            if (cameraController == null)
            {
                cameraController = mainCamera.gameObject.AddComponent<CameraController>();
            }
            else
            {
                Debug.Log("[GameInitializer] MainCamera already has CameraController.");
            }
            if (!cameraController.enabled)
            {
                Debug.Log("[GameInitializer] CameraController is disabled, waiting for player to spawn.");
            }
        }

        // Ensure NetworkController exists and is active
        GameObject networkController = GameObject.Find("NetworkController");
        if (networkController == null)
        {
            networkController = new GameObject("NetworkController");
            Debug.Log("[GameInitializer] Created NetworkController GameObject.");
        }
        if (!networkController.activeInHierarchy)
        {
            networkController.SetActive(true);
            Debug.Log("[GameInitializer] Activated NetworkController GameObject.");
        }

        // Ensure WebSocketPlayerController exists and is enabled
        WebSocketPlayerController wsController = networkController.GetComponent<WebSocketPlayerController>();
        if (wsController == null)
        {
            wsController = networkController.AddComponent<WebSocketPlayerController>();
            Debug.Log("[GameInitializer] Added WebSocketPlayerController to NetworkController.");
        }
        if (!wsController.enabled)
        {
            wsController.enabled = true;
            Debug.Log("[GameInitializer] Enabled WebSocketPlayerController component.");
        }

        // Initialize NetworkManager
        NetworkManagerInstance = InkaNetworkManager.CreateInstance(serverUri);
        Debug.Log($"[GameInitializer] NetworkManager created with URI: {serverUri}");
        yield return NetworkManagerInstance.ConnectAsync();
        Debug.Log("[GameInitializer] Network connected.");

        Debug.Log("[GameInitializer] Initialization complete.");
    }

    private void OnDestroy()
    {
        NetworkManagerInstance?.Dispose();
        NetworkManagerInstance = null;
        PlayerManagerInstance = null;
        GameManagerInstance = null;
    }
}