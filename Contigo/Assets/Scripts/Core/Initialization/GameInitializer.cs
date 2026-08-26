// using UnityEngine;
// using System.Collections;
// using Core.Networking;
// using Gameplay.InkaCamera;
// using Gameplay.Managers;
//
// namespace Core.Initialization
// {
//
//     public class GameInitializer : MonoBehaviour
//     {
//         public static InkaNetworkManager NetworkManagerInstance { get; private set; }
//         public static PlayerManager PlayerManagerInstance { get; private set; }
//         public static GameManager GameManagerInstance { get; private set; }
//
//         [SerializeField] private string serverUri = "wss://api.inkaverse.co/ws";
//         [SerializeField] private GameObject playerPrefab;
//         [SerializeField] private Camera mainCamera;
//
//         private void Awake()
//         {
//             StartCoroutine(InitializeGame());
//         }
//
//         private IEnumerator InitializeGame()
//         {
//             // Initialize PlayerManager with the player prefab
//             PlayerManagerInstance = gameObject.AddComponent<PlayerManager>();
//             PlayerManagerInstance.Initialize(playerPrefab);
//             Debug.Log("[GameInitializer] PlayerManager instance created.");
//
//             // Initialize GameManager
//             GameManagerInstance = gameObject.AddComponent<GameManager>();
//             GameManagerInstance.Initialize(serverUri, playerPrefab);
//             Debug.Log("[GameInitializer] GameManager instance created.");
//
//             // Setup camera
//             if (mainCamera != null)
//             {
//                 CameraController cameraController = mainCamera.GetComponent<CameraController>();
//                 if (cameraController == null)
//                 {
//                     cameraController = mainCamera.gameObject.AddComponent<CameraController>();
//                 }
//                 else
//                 {
//                     Debug.Log("[GameInitializer] MainCamera already has CameraController.");
//                 }
//                 if (!cameraController.enabled)
//                 {
//                     Debug.Log("[GameInitializer] CameraController is disabled, waiting for player to spawn.");
//                 }
//             }
//
//             // Ensure NetworkController exists and is active
//             GameObject networkController = GameObject.Find("NetworkController");
//             if (networkController == null)
//             {
//                 networkController = new GameObject("NetworkController");
//                 Debug.Log("[GameInitializer] Created NetworkController GameObject.");
//             }
//             if (!networkController.activeInHierarchy)
//             {
//                 networkController.SetActive(true);
//                 Debug.Log("[GameInitializer] Activated NetworkController GameObject.");
//             }
//
//             // Ensure WebSocketPlayerController exists and is enabled
//             WebSocketPlayerController wsController = networkController.GetComponent<WebSocketPlayerController>();
//             if (wsController == null)
//             {
//                 wsController = networkController.AddComponent<WebSocketPlayerController>();
//                 Debug.Log("[GameInitializer] Added WebSocketPlayerController to NetworkController.");
//             }
//             if (!wsController.enabled)
//             {
//                 wsController.enabled = true;
//                 Debug.Log("[GameInitializer] Enabled WebSocketPlayerController component.");
//             }
//
//             // Initialize NetworkManager
//             NetworkManagerInstance = InkaNetworkManager.CreateInstance(serverUri);
//             Debug.Log($"[GameInitializer] NetworkManager created with URI: {serverUri}");
//             yield return NetworkManagerInstance.ConnectAsync();
//             Debug.Log("[GameInitializer] Network connected.");
//
//             Debug.Log("[GameInitializer] Initialization complete.");
//         }
//
//         private void OnDestroy()
//         {
//             NetworkManagerInstance?.Dispose();
//             NetworkManagerInstance = null;
//             PlayerManagerInstance = null;
//             GameManagerInstance = null;
//         }
//     }
//     
// }

//
// using System.Collections;
// using UnityEngine;
// using Core.Networking;
// using Gameplay.Managers;
// using InkaCamera;
// using Core.Input;
//
// namespace Core.Initialization 
// {
//     public class GameInitializer : MonoBehaviour
//     {
//         [SerializeField] private string serverUri = "wss://api.inkaverse.co/ws"; 
//         [SerializeField] private GameObject playerPrefab;
//         [SerializeField] private Camera mainCamera;
//         [SerializeField] private GameObject uiCanvasControllerPrefab;
//
//         private InkaNetworkManager _networkManager;
//         private PlayerManager _playerManager;
//         private GameManager _gameManager;
//         private CameraController _cameraController;
//         private WebSocketPlayerController _wsController;
//         private UICanvasControllerInput _uiCanvasController;
//         private RemotePlayerManager _remotePlayerManager;
//
//         private enum InitializationState
//         {
//             NotStarted,
//             NetworkConnecting,
//             NetworkConnected,
//             PlayerManager,
//             RemotePlayerManager,
//             GameManager,
//             UICanvasController,
//             WebSocketPlayerController,
//             PlayerSpawning,
//             PlayerSpawned,
//             CameraSetup,
//             Complete
//         }
//
//         private InitializationState _state = InitializationState.NotStarted;
//         private float _networkConnectionStartTime;
//         private const float NetworkConnectionTimeout = 10f;
//
//         public InkaNetworkManager NetworkManager => _networkManager;
//         public PlayerManager PlayerManager => _playerManager;
//         public GameManager GameManager => _gameManager;
//
//         private void Start()
//         {
//             _state = InitializationState.NotStarted;
//         }
//
//         private void Update()
//         {
//             switch (_state)
//             {
//                 case InitializationState.NotStarted:
//                     _networkManager = InkaNetworkManager.CreateInstance(serverUri);
//                     StartCoroutine(ConnectNetworkManager());
//                     _networkConnectionStartTime = Time.time;
//                     _state = InitializationState.NetworkConnecting;
//                     break;
//
//                 case InitializationState.NetworkConnecting:
//                     if (_networkManager.IsConnected)
//                     {
//                         _state = InitializationState.NetworkConnected;
//                     }
//                     else if (Time.time - _networkConnectionStartTime >= NetworkConnectionTimeout)
//                     {
//                         Debug.LogError($"[GameInitializer] Failed to connect to server after {NetworkConnectionTimeout} seconds.");
//                         _state = InitializationState.Complete;
//                     }
//                     break;
//
//                 case InitializationState.NetworkConnected:
//                     _state = InitializationState.PlayerManager;
//                     break;
//
//                 case InitializationState.PlayerManager:
//                     _playerManager = gameObject.AddComponent<PlayerManager>();
//                     _playerManager.Initialize(playerPrefab, _networkManager);
//                     _state = InitializationState.RemotePlayerManager;
//                     break;
//
//                 case InitializationState.RemotePlayerManager:
//                     _remotePlayerManager = gameObject.AddComponent<RemotePlayerManager>();
//                     _remotePlayerManager.Initialize(playerPrefab);
//                     _state = InitializationState.GameManager;
//                     break;
//
//                 case InitializationState.GameManager:
//                     _gameManager = gameObject.AddComponent<GameManager>();
//                     _gameManager.Initialize(_networkManager, _playerManager, _remotePlayerManager);
//                     _state = InitializationState.UICanvasController;
//                     break;
//
//                 case InitializationState.UICanvasController:
//                     GameObject uiCanvasControllerObj = GameObject.Find("UI_Canvas_StarterAssetsInputs_Joysticks");
//                     if (uiCanvasControllerObj == null && uiCanvasControllerPrefab != null)
//                     {
//                         uiCanvasControllerObj = Instantiate(uiCanvasControllerPrefab);
//                         uiCanvasControllerObj.name = "UI_Canvas_StarterAssetsInputs_Joysticks";
//                     }
//                     if (uiCanvasControllerObj != null)
//                     {
//                         _uiCanvasController = uiCanvasControllerObj.GetComponent<UICanvasControllerInput>();
//                         if (_uiCanvasController == null)
//                         {
//                             _uiCanvasController = uiCanvasControllerObj.AddComponent<UICanvasControllerInput>();
//                         }
//                         _uiCanvasController.Initialize(_playerManager);
//                     }
//                     _state = InitializationState.WebSocketPlayerController;
//                     break;
//
//                 case InitializationState.WebSocketPlayerController:
//                     GameObject networkController = GameObject.Find("NetworkController");
//                     if (networkController == null)
//                     {
//                         networkController = new GameObject("NetworkController");
//                     }
//                     if (!networkController.activeInHierarchy)
//                     {
//                         networkController.SetActive(true);
//                     }
//                     _wsController = networkController.GetComponent<WebSocketPlayerController>();
//                     if (_wsController == null)
//                     {
//                         _wsController = networkController.AddComponent<WebSocketPlayerController>();
//                     }
//                     _wsController.Initialize(_networkManager, _playerManager, _remotePlayerManager);
//                     _state = InitializationState.PlayerSpawning;
//                     break;
//
//                 case InitializationState.PlayerSpawning:
//                     if (_playerManager.LocalPlayer != null)
//                     {
//                         _state = InitializationState.PlayerSpawned;
//                     }
//                     break;
//
//                 case InitializationState.PlayerSpawned:
//                     Camera mainCameraInstance = Camera.main;
//                     if (mainCameraInstance == null)
//                     {
//                         GameObject cameraObject = new GameObject("MainCamera");
//                         mainCameraInstance = cameraObject.AddComponent<Camera>();
//                         cameraObject.tag = "MainCamera";
//                         Debug.Log("[GameInitializer] Created new MainCamera as none was found.");
//                     }
//                     _cameraController = mainCameraInstance.GetComponent<CameraController>();
//                     if (_cameraController == null)
//                     {
//                         _cameraController = mainCameraInstance.gameObject.AddComponent<CameraController>();
//                         Debug.Log("[GameInitializer] Added CameraController to MainCamera.");
//                     }
//                     _cameraController.Initialize(_playerManager);
//                     _state = InitializationState.Complete;
//                     break;
//
//                 case InitializationState.Complete:
//                     enabled = false;
//                     break;
//             }
//         }
//
//         private IEnumerator ConnectNetworkManager()
//         {
//             yield return _networkManager.ConnectAsync();
//         }
//
//         private void OnDestroy()
//         {
//             _networkManager?.Dispose();
//         }
//     }
// }


using System.Collections;
using UnityEngine;
using Core.Networking;
using Gameplay.Managers;
using InkaCamera;
using Core.Input;

namespace Core.Initialization 
{
    public class GameInitializer : MonoBehaviour
    {
        [Tooltip("Leave empty. The endpoint is resolved automatically: localhost in the Editor, by page hostname in WebGL builds, or ?server= on the URL. Fill only to force a specific server.")]
        [SerializeField] private string serverUriOverride = "";
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
            CameraSetup,
            Complete
        }

        private InitializationState _state = InitializationState.NotStarted;
        private float _networkConnectionStartTime;
        private const float NetworkConnectionTimeout = 10f;

        public InkaNetworkManager NetworkManager => _networkManager;
        public PlayerManager PlayerMåanager => _playerManager;
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
                    _networkManager = InkaNetworkManager.CreateInstance(ServerEndpoint.Resolve(serverUriOverride));
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
                        Debug.LogError($"[GameInitializer] Failed to connect to server after {NetworkConnectionTimeout} seconds.");
                        _state = InitializationState.Complete;
                    }
                    break;

                case InitializationState.SubscribedToEvents:
                    GameObject networkController = GameObject.Find("NetworkController") ?? new GameObject("NetworkController");
                    if (!networkController.activeInHierarchy)
                        networkController.SetActive(true);

                    _wsController = networkController.GetComponent<WebSocketPlayerController>() ?? networkController.AddComponent<WebSocketPlayerController>();
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
                    GameObject uiCanvasControllerObj = GameObject.Find("UI_Canvas_StarterAssetsInputs_Joysticks");
                    if (uiCanvasControllerObj == null && uiCanvasControllerPrefab != null)
                    {
                        uiCanvasControllerObj = Instantiate(uiCanvasControllerPrefab);
                        uiCanvasControllerObj.name = "UI_Canvas_StarterAssetsInputs_Joysticks";
                    }
                    if (uiCanvasControllerObj != null)
                    {
                        _uiCanvasController = uiCanvasControllerObj.GetComponent<UICanvasControllerInput>() ?? uiCanvasControllerObj.AddComponent<UICanvasControllerInput>();
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
                    _cameraController = mainCameraInstance.GetComponent<CameraController>() ?? mainCameraInstance.gameObject.AddComponent<CameraController>();
                    _cameraController.Initialize(_playerManager);
                    _state = InitializationState.Complete;
                    break;

                case InitializationState.Complete:
                    Debug.Log("✅ [GameInitializer] Initialization Complete.");
                    enabled = false;
                    break;
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
