using UnityEngine;
using Managers;
using ClientPlayerData;
using System.Threading.Tasks;

namespace Controllers
{
    public class WebSocketPlayerController : MonoBehaviour
    {
        [SerializeField] private string serverUri = "ws://localhost:6000/ws";
        [SerializeField] private GameObject playerPrefab;
        [SerializeField] private float localSpeed = 5.0f;
        [SerializeField] private float serverTimestep = 0.05f; // 20 Hz

        private WebSocketManager _networkManager;
        private PlayerPhysicsManager _physicsManager;
        private RemotePlayerManager _remoteManager;
        private CameraController _cameraController;
        private string _localId;
        private GameObject _localPlayer;
        private Snapshot _latestSnapshot; // Store latest snapshot for FixedUpdate

        private void Start()
        {
            Application.runInBackground = true;
            QualitySettings.vSyncCount = 0;
            Application.targetFrameRate = 60;
            Time.fixedDeltaTime = serverTimestep;

            _networkManager = new WebSocketManager(serverUri);
            _remoteManager = new RemotePlayerManager(playerPrefab);

            GameObject cameraObj = Camera.main != null ? Camera.main.gameObject : new GameObject("MainCamera");
            if (!cameraObj.GetComponent<Camera>()) cameraObj.AddComponent<Camera>();
            _cameraController = cameraObj.GetComponent<CameraController>() ?? cameraObj.AddComponent<CameraController>();

            _networkManager.OnIdReceived += OnIdReceived;
            _networkManager.OnSnapshotReceived += OnSnapshotReceived;

            ConnectWebSocketAsync();
            Debug.Log("WebSocketPlayerController initialized.");
        }

        private async void ConnectWebSocketAsync()
        {
            await _networkManager.ConnectAsync();
            Debug.Log("WebSocket connected.");
        }

        private void OnIdReceived(string id)
        {
            _localId = id;
            if (_localPlayer == null)
            {
                _localPlayer = Instantiate(playerPrefab);
                _physicsManager = new PlayerPhysicsManager(_localPlayer, localSpeed, serverTimestep, _networkManager);
                _cameraController.target = _localPlayer.transform;
                Debug.Log($"Local player spawned with ID: {_localId}");
            }
        }

        private void FixedUpdate()
        {
            _physicsManager?.UpdatePhysics(_latestSnapshot, _localId); // Pass snapshot for collision
            _remoteManager?.ApplyServerPositions();
            _networkManager?.DispatchMessageQueue();
        }

        private void Update()
        {
            _physicsManager?.InterpolateLocalPlayer();
            _remoteManager?.InterpolateRemotePlayers();
            _cameraController?.UpdateCamera();
        }

        private void OnSnapshotReceived(Snapshot snapshot)
        {
            if (snapshot == null || snapshot.Positions == null)
            {
                Debug.LogWarning("OnSnapshotReceived: Received invalid snapshot.");
                return;
            }

            Debug.Log($"Snapshot received with {snapshot.Positions.Count} players at timestamp: {snapshot.Timestamp}");
            _latestSnapshot = snapshot; // Store for FixedUpdate
            _remoteManager.StoreSnapshot(snapshot, _localId);
        }

        private void OnApplicationQuit()
        {
            _networkManager?.Close();
        }
    }
}