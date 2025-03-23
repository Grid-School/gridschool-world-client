using System;
using UnityEngine;
using Managers;
using ClientPlayerData;
using System.Threading.Tasks;
using StarterAssets;
using UnityEngine.UI;
using Newtonsoft.Json;

namespace Controllers
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
        private ThirdPersonController _localController;
        private StarterAssetsInputs _localInputs;
        private Snapshot _latestSnapshot;

        private void Awake()
        {
            jumpButton = GameObject.Find("UI_Virtual_Button_Jump")?.GetComponent<Button>();
            sprintButton = GameObject.Find("UI_Virtual_Button_Sprint")?.GetComponent<Button>();

            if (jumpButton == null || sprintButton == null)
            {
                Debug.LogError("One or more UI elements not found in the scene!");
            }
            else
            {
                Debug.Log("UI elements (jump and sprint buttons) found in the scene.");
            }
        }

        private async void Start()
        {
            Application.runInBackground = true;
            QualitySettings.vSyncCount = 0;
            Application.targetFrameRate = 60;
            Time.fixedDeltaTime = serverTimestep;

            if (playerPrefab == null)
            {
                Debug.LogError("Player Prefab is not assigned in the Inspector!");
                return;
            }

            _networkManager = new WebSocketManager(serverUri);
            _remoteManager = new RemotePlayerManager(playerPrefab);

            _networkManager.OnIdReceived += OnIdReceived;
            _networkManager.OnSnapshotReceived += OnSnapshotReceived;

            Debug.Log("Attempting to connect to WebSocket...");
            await ConnectWebSocketAsync();
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
            Debug.Log($"OnIdReceived called with ID: {id}");
            _localId = id;
            if (_localPlayer == null)
            {
                Debug.Log("Instantiating local player...");
                _localPlayer = Instantiate(playerPrefab, new Vector3(0, 1, 0), Quaternion.identity);
                _localController = _localPlayer.GetComponent<ThirdPersonController>();
                _localInputs = _localPlayer.GetComponent<StarterAssetsInputs>();

                if (_localController == null || _localInputs == null)
                {
                    Debug.LogError("ThirdPersonController or StarterAssetsInputs not found on local player prefab!");
                    return;
                }

                // Set up the CameraController
                var mainCamera = Camera.main;
                if (mainCamera != null)
                {
                    var cameraController = mainCamera.GetComponent<CameraController>();
                    if (cameraController != null)
                    {
                        cameraController.target = _localPlayer.transform;
                        Debug.Log($"Set CameraController target to {_localPlayer.name}");
                    }
                    else
                    {
                        Debug.LogError("CameraController not found on MainCamera!");
                    }
                }
                else
                {
                    Debug.LogError("MainCamera not found in the scene!");
                }

                var uiCanvasController = GameObject.Find("UI_Canvas_StarterAssetsInputs_Joysticks")?.GetComponent<UICanvasControllerInput>();
                if (uiCanvasController != null)
                {
                    uiCanvasController.starterAssetsInputs = _localInputs;
                    Debug.Log("UI canvas controller connected to StarterAssetsInputs.");
                }
                else
                {
                    Debug.LogWarning("UI_Canvas_StarterAssetsInputs_Joysticks or UICanvasControllerInput not found in the scene!");
                }

                ConnectUIElements();

                Debug.Log($"Local player spawned with ID: {_localId} at position: {_localPlayer.transform.position}");
            }
        }

        private void ConnectUIElements()
        {
            if (jumpButton != null)
            {
                jumpButton.onClick.RemoveAllListeners();
                jumpButton.onClick.AddListener(() =>
                {
                    if (_localInputs != null)
                    {
                        _localInputs.jump = true;
                    }
                });
                Debug.Log("Jump button connected.");
            }
            if (sprintButton != null)
            {
                sprintButton.onClick.RemoveAllListeners();
                sprintButton.onClick.AddListener(() =>
                {
                    if (_localInputs != null)
                    {
                        _localInputs.sprint = true;
                    }
                });
                Debug.Log("Sprint button connected.");
            }
        }

        private void Update()
        {
            if (_localInputs != null)
            {
                _localInputs.jump = false;
                _localInputs.sprint = false;
            }

            _remoteManager?.InterpolateRemotePlayers();
            _networkManager?.DispatchMessageQueue();
        }

        private void FixedUpdate()
        {
            if (_networkManager == null || _localController == null || _localPlayer == null) return;

            SendPlayerData();
            _remoteManager?.ApplyServerPositions();
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
                MotionSpeed = _localController.MotionSpeed, // Added to match updated InputMessage
                Jump = _localController.IsJumping,
                Grounded = _localController.Grounded,
                FreeFall = !_localController.Grounded
            };

            Debug.Log($"Sending player data - Position: ({inputMessage.X}, {inputMessage.Y}, {inputMessage.Z}), Angle: {inputMessage.Angle}, Speed: {inputMessage.Speed}, MotionSpeed: {inputMessage.MotionSpeed}, Jump: {inputMessage.Jump}, Grounded: {inputMessage.Grounded}, FreeFall: {inputMessage.FreeFall}");

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
            _latestSnapshot = snapshot;

            Debug.Log($"Snapshot received with timestamp: {snapshot.Timestamp}, Positions count: {snapshot.Positions.Count}");
            if (snapshot.Positions.ContainsKey(_localId))
            {
                var positionData = snapshot.Positions[_localId];
                Debug.Log($"Position for local player {_localId}: {positionData.ToVector3()}, Angle: {positionData.Angle}");
            }
            else
            {
                Debug.LogWarning($"No position found for local player {_localId} in snapshot.");
            }

            // Log animation data for all players in the snapshot
            if (snapshot.Animations != null)
            {
                foreach (var kvp in snapshot.Animations)
                {
                    var animState = kvp.Value;
                    Debug.Log($"Animation state for player {kvp.Key}: Speed: {animState.Speed}, MotionSpeed: {animState.MotionSpeed}, Jump: {animState.Jump}, Grounded: {animState.Grounded}, FreeFall: {animState.FreeFall}");
                }
            }

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

        private void OnApplicationQuit()
        {
            _networkManager?.Close();
        }
    }
}