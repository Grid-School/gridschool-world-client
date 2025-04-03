using UnityEngine;
using Gameplay.Managers;
using Core.Networking;

[DefaultExecutionOrder(-200)]
public class GameInitializer : MonoBehaviour
{
    [SerializeField] private GameObject playerPrefab;
    // [SerializeField] private string serverUri = "ws://localhost:6000/ws";
    [SerializeField] private string serverUri = "wss://api.inkaverse.co/ws";

    public static PlayerManager PlayerManagerInstance { get; private set; }
    public static InkaNetworkManager NetworkManagerInstance { get; private set; }
    public static GameManager GameManagerInstance { get; private set; }

    private async void Awake()
    {
        if (playerPrefab == null)
        {
            Debug.LogError("[GameInitializer] PlayerPrefab is not assigned!");
            return;
        }
        
        Application.runInBackground = true;

        // Create PlayerManager
        GameObject managerObj = new GameObject("PlayerManager");
        PlayerManagerInstance = managerObj.AddComponent<PlayerManager>();
        PlayerManagerInstance.playerPrefab = playerPrefab;
        DontDestroyOnLoad(managerObj);
        Debug.Log("[GameInitializer] PlayerManager instance created.");

        // Create GameManager
        GameObject gameManagerObj = new GameObject("GameManager");
        GameManagerInstance = gameManagerObj.AddComponent<GameManager>();
        GameManagerInstance.Initialize(serverUri, playerPrefab); // Pass necessary data
        DontDestroyOnLoad(gameManagerObj);
        Debug.Log("[GameInitializer] GameManager instance created.");

        // Create NetworkManager and connect
        NetworkManagerInstance = InkaNetworkManager.CreateInstance(serverUri);
        Debug.Log($"[GameInitializer] NetworkManager created with URI: {serverUri}");

        try
        {
            await NetworkManagerInstance.ConnectAsync();
            Debug.Log("[GameInitializer] Network connected.");
        }
        catch (System.Exception ex)
        {
            Debug.LogError("[GameInitializer] Network connection failed: " + ex.Message);
        }
    }
}