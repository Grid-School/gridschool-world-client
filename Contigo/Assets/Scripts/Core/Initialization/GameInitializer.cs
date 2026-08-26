
using System.Collections;
using UnityEngine;
using Core.Networking;
using Gameplay.Managers;
using InkaCamera;
using Core.Input;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;

namespace Core.Initialization 
{
    public class GameInitializer : MonoBehaviour
    {
        [SerializeField] private string serverUri = "wss://api.inkaverse.co/ws"; 
        [SerializeField] private GameObject playerPrefab;
        [SerializeField] private Camera mainCamera;
        [SerializeField] private GameObject uiCanvasControllerPrefab;

        private InkaNetworkManager _networkManager;
        private PlayerManager _playerManager;
        private GameManager _gameManager;
        private CameraController _cameraController;
        private WebSocketPlayerController _wsController;
        private UICanvasControllerInput _uiCanvasController;
        private RemotePlayerManager _remotePlayerManager;

        private enum InitializationState
        {
            NotStarted,
            NetworkConnecting,
            NetworkConnected,
            SubscribedToEvents,
            PlayerManager,
            RemotePlayerManager,
            GameManager,
            UICanvasController,
            WebSocketPlayerController,
            PlayerSpawning,
            PlayerSpawned,
            PlanetLoading,
            CameraSetup,
            Complete
        }

        private InitializationState _state = InitializationState.NotStarted;
        private float _networkConnectionStartTime;
        private const float NetworkConnectionTimeout = 10f;

        public InkaNetworkManager NetworkManager => _networkManager;
        public PlayerManager PlayerManager => _playerManager;
        public GameManager GameManager => _gameManager;

        private void Start()
        {
            _state = InitializationState.NotStarted;
        }
        
        void Awake()
        {
            Application.runInBackground = true;
        }

        private void Update()
        {
            switch (_state)
            {
                case InitializationState.NotStarted:
                    _networkManager = InkaNetworkManager.CreateInstance(serverUri);
                    _networkConnectionStartTime = Time.time;
                    StartCoroutine(ConnectNetworkManager());
                    _state = InitializationState.NetworkConnecting;
                    break;

                case InitializationState.NetworkConnecting:
                    if (_networkManager.IsConnected)
                    {
                        _state = InitializationState.SubscribedToEvents;
                    }
                    else if (Time.time - _networkConnectionStartTime >= NetworkConnectionTimeout)
                    {
                        Debug.LogError(
                            $"[GameInitializer] Failed to connect to server after {NetworkConnectionTimeout} seconds.");
                        _state = InitializationState.Complete;
                    }

                    break;

                case InitializationState.SubscribedToEvents:
                    GameObject networkController =
                        GameObject.Find("NetworkController") ?? new GameObject("NetworkController");
                    if (!networkController.activeInHierarchy)
                        networkController.SetActive(true);

                    _wsController = networkController.GetComponent<WebSocketPlayerController>() ??
                                    networkController.AddComponent<WebSocketPlayerController>();
                    _playerManager = gameObject.AddComponent<PlayerManager>();
                    _playerManager.Initialize(playerPrefab, _networkManager);

                    _remotePlayerManager = gameObject.AddComponent<RemotePlayerManager>();
                    _remotePlayerManager.Initialize(playerPrefab);

                    _wsController.Initialize(_networkManager, _playerManager, _remotePlayerManager);

                    _state = InitializationState.GameManager;
                    break;

                case InitializationState.GameManager:
                    _gameManager = gameObject.AddComponent<GameManager>();
                    _gameManager.Initialize(_networkManager, _playerManager, _remotePlayerManager);
                    _state = InitializationState.UICanvasController;
                    break;

                case InitializationState.UICanvasController:
                    SetupEventSystem();
                    GameObject uiCanvasControllerObj = GameObject.Find("UI_Canvas_StarterAssetsInputs_Joysticks");
                    if (uiCanvasControllerObj == null && uiCanvasControllerPrefab != null)
                    {
                        uiCanvasControllerObj = Instantiate(uiCanvasControllerPrefab);
                        uiCanvasControllerObj.name = "UI_Canvas_StarterAssetsInputs_Joysticks";
                    }

                    if (uiCanvasControllerObj != null)
                    {
                        _uiCanvasController = uiCanvasControllerObj.GetComponent<UICanvasControllerInput>() ??
                                              uiCanvasControllerObj.AddComponent<UICanvasControllerInput>();
                        _uiCanvasController.Initialize(_playerManager);
                    }

                    _state = InitializationState.PlayerSpawning;
                    break;

                case InitializationState.PlayerSpawning:
                    if (_playerManager.LocalPlayer != null)
                    {
                        _state = InitializationState.PlayerSpawned;
                    }

                    break;

                case InitializationState.PlayerSpawned:
                    Camera mainCameraInstance = Camera.main;
                    if (mainCameraInstance == null)
                    {
                        GameObject cameraObject = new GameObject("MainCamera");
                        mainCameraInstance = cameraObject.AddComponent<Camera>();
                        cameraObject.tag = "MainCamera";
                        Debug.Log("[GameInitializer] Created new MainCamera as none was found.");
                    }

                    _cameraController = mainCameraInstance.GetComponent<CameraController>() ??
                                        mainCameraInstance.gameObject.AddComponent<CameraController>();
                    _cameraController.Initialize(_playerManager);
                    _state = InitializationState.PlanetLoading;
                    break;
                
                case InitializationState.PlanetLoading:
                    // Instead of CreatePrimitive, _only_ create the manager:
                    var pmGo = new GameObject("PlanetManager");
                    var pm = pmGo.AddComponent<PlanetManager>();
                    pm.PlanetCenter = GameObject.Find("Planet").transform;  // your scene sphere
                    _state = InitializationState.Complete; 
                    break;
                
                case InitializationState.Complete:
                    Debug.Log("✅ [GameInitializer] Initialization Complete.");
                    enabled = false;
                    break;
            }
        }
        
        private void SetupEventSystem()
        {
            EventSystem eventSystem = FindObjectOfType<EventSystem>();
            if (eventSystem == null)
            {
                GameObject eventSystemObj = new GameObject("EventSystem");
                eventSystem = eventSystemObj.AddComponent<EventSystem>();
            }

            // Remove StandaloneInputModule if present
            var standaloneInputModule = eventSystem.GetComponent<StandaloneInputModule>();
            if (standaloneInputModule != null)
            {
                Destroy(standaloneInputModule);
            }

            // Add InputSystemUIInputModule if not present
            if (eventSystem.GetComponent<InputSystemUIInputModule>() == null)
            {
                eventSystem.gameObject.AddComponent<InputSystemUIInputModule>();
            }
        }

        private void LateUpdate()
        {
            if (_playerManager?.LocalPlayerTransform != null && (_cameraController == null || !_cameraController.enabled))
            {
                Debug.LogWarning("[GameInitializer] Late binding CameraController.");
                var cam = Camera.main ?? Instantiate(new GameObject("MainCamera")).AddComponent<Camera>();
                cam.tag = "MainCamera";
                _cameraController = cam.GetComponent<CameraController>() ?? cam.gameObject.AddComponent<CameraController>();
                _cameraController.Initialize(_playerManager);
            }
        }
        
        private IEnumerator ConnectNetworkManager()
        {
            yield return _networkManager.ConnectAsync();
        }

        private void OnDestroy()
        {
            _networkManager?.Dispose();
        }
    }
}
