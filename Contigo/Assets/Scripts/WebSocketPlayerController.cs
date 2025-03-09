using UnityEngine;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using NativeWebSocket;
using Newtonsoft.Json;

public class WebSocketPlayerController : MonoBehaviour
{
    private WebSocket websocket;
    private string serverUri = "ws://127.0.0.1:6000/ws";
    private CancellationTokenSource cancellationTokenSource;

    public GameObject playerPrefab;
    private Dictionary<string, GameObject> remotePlayers = new Dictionary<string, GameObject>();
    private Dictionary<string, Vector3> remotePlayerVelocities = new Dictionary<string, Vector3>();

    private string localId = null;
    private GameObject localPlayer;
    private Camera mainCamera;

    private float lastSendTime = 0;
    private const float SendInterval = 0.05f; // 20 updates/sec
    public float localSpeed = 5.0f;

    private bool isShuttingDown = false;

    async void Start()
    {
        Application.runInBackground = true;

        // Performance setup: Target 60 FPS, optimize for WebGL
        QualitySettings.vSyncCount = 0; // Disable VSync (unsupported in WebGL)
        Application.targetFrameRate = 60; // Cap at 60 FPS for consistency and battery life

        #if UNITY_WEBGL
            QualitySettings.antiAliasing = 0; // Reduce GPU load
            QualitySettings.shadowResolution = ShadowResolution.Low;
            QualitySettings.shadowDistance = 20f;
            QualitySettings.pixelLightCount = 1;
        #endif

        Debug.Log("Initialized with 60 FPS target.");

        cancellationTokenSource = new CancellationTokenSource();
        
        websocket = new WebSocket(serverUri);
        mainCamera = Camera.main;

        websocket.OnOpen += () => Debug.Log("Connected to WebSocket Server!");
        websocket.OnMessage += (bytes) => HandleServerMessage(System.Text.Encoding.UTF8.GetString(bytes));
        websocket.OnClose += (closeCode) =>
        {
            Debug.Log($"WebSocket closed with code: {closeCode}");
            Cleanup();
        };
        websocket.OnError += (error) => Debug.LogError($"WebSocket error: {error}");

        await websocket.Connect();
        StartReceiveLoop();
        
        _ = ContinuousPositionUpdate();
    }

    async void StartReceiveLoop()
    {
        if (cancellationTokenSource == null)
        {
            if (!isShuttingDown)
                Debug.LogError("CancellationTokenSource is null in StartReceiveLoop.");
            return;
        }

        try
        {
            while (!cancellationTokenSource.Token.IsCancellationRequested)
            {
                websocket.DispatchMessageQueue();
                await Task.Delay(5, cancellationTokenSource.Token); // ~200 updates/sec
            }
            Debug.Log("Receive loop exited.");
        }
        catch (TaskCanceledException)
        {
            Debug.Log("Receive loop canceled.");
        }
        catch (System.Exception ex)
        {
            Debug.LogError($"Receive loop error: {ex.Message}");
        }
    }

    void FixedUpdate()
    {
        if (websocket != null)
            websocket.DispatchMessageQueue();
    }

    void Update()
    {
        HandlePlayerInput();
        if (localPlayer != null)
            FollowPlayer();
    }

    async void HandlePlayerInput()
    {
        if (websocket.State != WebSocketState.Open || localPlayer == null) return;

        float h = Input.GetAxisRaw("Horizontal");
        float v = Input.GetAxisRaw("Vertical");
        Vector3 inputDirection = new Vector3(h, 0, v).normalized;

        if (inputDirection != Vector3.zero)
        {
            localPlayer.transform.position += inputDirection * localSpeed * Time.deltaTime;

            if (Time.time - lastSendTime > SendInterval)
            {
                InputMessage input = new InputMessage
                {
                    X = localPlayer.transform.position.x,
                    Y = localPlayer.transform.position.y,
                    Z = localPlayer.transform.position.z
                };
                await websocket.SendText(JsonConvert.SerializeObject(input));
                lastSendTime = Time.time;
            }
        }
    }

    private async Task ContinuousPositionUpdate()
    {
        if (cancellationTokenSource == null)
        {
            if (!isShuttingDown)
                Debug.LogError("CancellationTokenSource is null in ContinuousPositionUpdate.");
            return;
        }

        try
        {
            while (websocket != null && websocket.State == WebSocketState.Open)
            {
                if (localPlayer != null)
                {
                    InputMessage input = new InputMessage
                    {
                        X = localPlayer.transform.position.x,
                        Y = localPlayer.transform.position.y,
                        Z = localPlayer.transform.position.z
                    };
                    await websocket.SendText(JsonConvert.SerializeObject(input));
                }
                await Task.Delay((int)(SendInterval * 1000), cancellationTokenSource.Token);
            }
            Debug.Log("Position update exited.");
        }
        catch (TaskCanceledException)
        {
            Debug.Log("Position update canceled.");
        }
        catch (System.Exception ex)
        {
            Debug.LogError($"Position update error: {ex.Message}");
        }
    }

    void HandleServerMessage(string json)
    {
        var message = JsonConvert.DeserializeObject<Dictionary<string, object>>(json);

        if (message.ContainsKey("type") && message["type"].ToString() == "ID")
        {
            localId = message["id"].ToString();
            if (localPlayer == null)
            {
                localPlayer = Instantiate(playerPrefab);
                Debug.Log($"Local player instantiated with ID: {localId}");
            }
        }
        else
        {
            var snapshot = JsonConvert.DeserializeObject<Dictionary<string, PositionData>>(json);
            if (snapshot == null) return;

            List<string> playersToRemove = new List<string>();
            foreach (var id in remotePlayers.Keys)
            {
                if (!snapshot.ContainsKey(id))
                    playersToRemove.Add(id);
            }

            foreach (var id in playersToRemove)
            {
                if (remotePlayers.TryGetValue(id, out var playerObj) && playerObj != null)
                    Destroy(playerObj);
                remotePlayers.Remove(id);
            }

            foreach (var kvp in snapshot)
            {
                string id = kvp.Key;
                Vector3 targetPos = new Vector3(kvp.Value.X, kvp.Value.Y, kvp.Value.Z);
                
                if (id != localId)
                {
                    if (!remotePlayers.ContainsKey(id) || remotePlayers[id] == null)
                    {
                        remotePlayers[id] = Instantiate(playerPrefab);
                        remotePlayers[id].transform.position = targetPos;
                    }
                    else
                    {
                        remotePlayers[id].transform.position = Vector3.Lerp(
                            remotePlayers[id].transform.position, targetPos, Time.deltaTime * 10);
                    }
                }
            }
        }
    }

    void FollowPlayer()
    {
        if (mainCamera != null && localPlayer != null)
        {
            mainCamera.transform.position = Vector3.Lerp(mainCamera.transform.position, 
                localPlayer.transform.position + new Vector3(0, 5, -5), Time.deltaTime * 5);
            mainCamera.transform.LookAt(localPlayer.transform);
        }
    }

    void OnDestroy()
    {
        Cleanup();
    }

    void OnApplicationQuit()
    {
        Cleanup();
    }

    private void Cleanup()
    {
        isShuttingDown = true;

        if (cancellationTokenSource != null)
        {
            cancellationTokenSource.Cancel();
            try
            {
                Task.Delay(100).Wait();
            }
            catch (System.Exception ex)
            {
                Debug.LogWarning($"Cleanup delay error: {ex.Message}");
            }
            cancellationTokenSource.Dispose();
            cancellationTokenSource = null;
        }

        if (websocket != null)
        {
            websocket.Close();
            websocket = null;
        }

        if (localPlayer != null)
        {
            Destroy(localPlayer);
            localPlayer = null;
        }

        foreach (var kvp in remotePlayers)
        {
            if (kvp.Value != null)
                Destroy(kvp.Value);
        }
        remotePlayers.Clear();
        remotePlayerVelocities.Clear();

        Debug.Log("Cleanup complete.");
    }
}

[System.Serializable]
public class InputMessage
{
    public float X { get; set; }
    public float Y { get; set; }
    public float Z { get; set; }
}

[System.Serializable]
public class PositionData
{
    public float X;
    public float Y;
    public float Z;
}