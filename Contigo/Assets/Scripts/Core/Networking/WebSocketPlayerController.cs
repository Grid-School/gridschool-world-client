using System;
using UnityEngine;
using System.Threading.Tasks;
using Core.Data.ClientPlayerData;
using Core.Input;
using Gameplay.Player;
using StarterAssets;
using UnityEngine.UI;
using Newtonsoft.Json;
using InkaCamera;

namespace Core.Networking 
{
    public class WebSocketPlayerController : MonoBehaviour
    {
        [SerializeField] private string serverUri = "ws://localhost:6000/ws";
        [SerializeField] private GameObject playerPrefab;
        [SerializeField] private float serverTimestep = 0.05f;

        [SerializeField] private Button jumpButton;
        [SerializeField] private Button sprintButton;

        private WebSocketManager _networkManager;
        private RemotePlayerManager _remoteManager;
        private string _localId;
        private GameObject _localPlayer;
        private PlayerController _localController;
        private PlayerCharacterInput _localInputs;
        private Snapshot _latestSnapshot;
        private bool _isDestroyed = false;

        private void Awake()
        {
            DontDestroyOnLoad(gameObject); // Prevent MPPM from destroying prematurely
        }

        private async void Start()
        {
            Application.runInBackground = true;
            QualitySettings.vSyncCount = 0;
            Application.targetFrameRate = 60;
            Time.fixedDeltaTime = serverTimestep;

            if (playerPrefab == null)
            {
                Debug.LogError("Player Prefab is not assigned!", this);
                return;
            }

            _networkManager = new WebSocketManager(serverUri);
            _remoteManager = new RemotePlayerManager(playerPrefab);

            _networkManager.OnIdReceived += OnIdReceived;
            _networkManager.OnSnapshotReceived += OnSnapshotReceived;

            Debug.Log($"Player instance starting: {gameObject.name}", this);
            await ConnectWebSocketAsync();
            // Remove single DispatchMessageQueue here; move to FixedUpdate
        }
        
        private async Task ConnectWebSocketAsync()
        {
            try
            {
                await _networkManager.ConnectAsync();
                Debug.Log("WebSocket connected.");
            }
            catch (Exception e)
            {
                Debug.LogError($"Failed to connect to WebSocket: {e.Message}");
            }
        }

        private void OnIdReceived(string id)
        {
            if (_isDestroyed || !gameObject.activeInHierarchy)
            {
                Debug.LogWarning($"OnIdReceived ignored: Controller destroyed or inactive for {gameObject.name}");
                return;
            }

            Debug.Log($"OnIdReceived called with ID: {id} on {gameObject.name}", this);
            _localId = id;
            if (_localPlayer == null)
            {
                _localPlayer = Instantiate(playerPrefab, new Vector3(0, 1, 0), Quaternion.identity);
                _localController = _localPlayer.GetComponent<PlayerController>();
                _localInputs = _localPlayer.GetComponent<PlayerCharacterInput>();

                if (_localController == null || _localInputs == null)
                {
                    Debug.LogError("Missing components on local player prefab!", this);
                    return;
                }

                SetupCameraAndUI();
                Debug.Log($"Local player spawned with ID: {_localId} at {_localPlayer.transform.position}", this);
            }
        }
        
        private void SetupCameraAndUI()
        {
            var mainCamera = Camera.main;
            if (mainCamera != null)
            {
                var cameraController = mainCamera.GetComponent<CameraController>();
                if (cameraController != null)
                {
                    cameraController.target = _localPlayer.transform;
                    Debug.Log($"Set CameraController target to {_localPlayer.name}", this);
                }
            }

            var uiCanvasObj = GameObject.Find("UI_Canvas_StarterAssetsInputs_Joysticks");
            if (uiCanvasObj != null)
            {
                var uiCanvasController = uiCanvasObj.GetComponent<UICanvasControllerInput>();
                if (uiCanvasController != null && _localInputs != null)
                {
                    uiCanvasController.starterAssetsInputs = _localInputs;
                    Debug.Log($"Connected UI canvas to Inputs", this);
                }
            }

            ConnectUIElements();
        }
        
        private void ConnectUIElements()
        {
            if (jumpButton != null)
            {
                jumpButton.onClick.RemoveAllListeners();
                jumpButton.onClick.AddListener(() =>
                {
                    if (_localInputs != null) _localInputs.jump = true;
                });
                Debug.Log("Jump button connected.");
            }
            if (sprintButton != null)
            {
                sprintButton.onClick.RemoveAllListeners();
                sprintButton.onClick.AddListener(() =>
                {
                    if (_localInputs != null) _localInputs.sprint = true;
                });
                Debug.Log("Sprint button connected.");
            }
        }

        private void Update()
        {
            _remoteManager?.InterpolateRemotePlayers();
        }

        private void FixedUpdate()
        {
            if (_networkManager == null || _isDestroyed) return;

            _networkManager.DispatchMessageQueue();
            if (_localController != null && _localPlayer != null)
            {
                SendPlayerData();
                _remoteManager?.ApplyServerPositions();
            }
        }

        private void LateUpdate() // Move resets here
        {
            if (_localInputs != null)
            {
                _localInputs.jump = false;
                _localInputs.sprint = false;
            }
        }

        private void SendPlayerData()
        {
            var inputMessage = new InputMessage
            {
                X = _localPlayer.transform.position.x,
                Y = _localPlayer.transform.position.y,
                Z = _localPlayer.transform.position.z,
                Angle = _localPlayer.transform.eulerAngles.y,
                Speed = _localController.CurrentSpeed,
                MotionSpeed = _localController.MotionSpeed,
                Jump = _localController.IsJumping,
                Grounded = _localController.Grounded,
                FreeFall = !_localController.Grounded
            };

            string json = JsonConvert.SerializeObject(inputMessage);
            _networkManager.SendMessage(json);
        }

        private void OnSnapshotReceived(Snapshot snapshot)
        {
            if (snapshot == null || snapshot.Positions == null)
            {
                Debug.LogWarning("OnSnapshotReceived: Received invalid snapshot.");
                return;
            }
            Debug.Log($"Snapshot received at {Time.time:F3}s");
            _latestSnapshot = snapshot;

            if (_localPlayer != null && _localPlayer.transform.position == new Vector3(0, 1, 0))
            {
                if (snapshot.Positions.ContainsKey(_localId))
                {
                    Vector3 spawnPos = snapshot.Positions[_localId].ToVector3();
                    _localPlayer.transform.position = spawnPos;
                    Debug.Log($"Set local player position to: {spawnPos}");
                }
            }

            _remoteManager.StoreSnapshot(snapshot, _localId);
        }
        
        private void OnDestroy()
        {
            _isDestroyed = true;
            _networkManager?.Close(); // Ensure this is the only close call
            Debug.Log($"WebSocketPlayerController destroyed: {gameObject.name}", this);
        }

        private void OnApplicationQuit()
        {
            _networkManager?.Close();
        }
    }
}