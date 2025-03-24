using Core.Networking;
using Unity.Netcode;
using UnityEngine;

namespace Gameplay.Managers
{
    public class GameManager : MonoBehaviour
    {
        [SerializeField] private GameObject playerPrefab;
        [SerializeField] private string serverUri = "ws://localhost:6000/ws";
        [SerializeField] private float serverTimestep = 0.05f;

        private InkaNetworkManager _networkManager;
        private PlayerManager _playerManager;
        private CameraAndUIManager _cameraUIManager;

        private static bool _isPrimaryController = true;

        void Awake()
        {
            if (!_isPrimaryController)
            {
                Destroy(gameObject);
                return;
            }

            _isPrimaryController = false;

            Application.runInBackground = true;
            QualitySettings.vSyncCount = 0;
            Application.targetFrameRate = 60;
            Time.fixedDeltaTime = serverTimestep;

            DontDestroyOnLoad(gameObject);
        }

        async void Start()
        {
            if (!ValidateSetup()) return;

            _networkManager = new InkaNetworkManager(serverUri);
            _playerManager = new PlayerManager(playerPrefab);
            _cameraUIManager = new CameraAndUIManager();

            _networkManager.OnIdReceived += _playerManager.SpawnLocalPlayer;
            _networkManager.OnSnapshotReceived += _playerManager.UpdateRemotePlayers;

            await _networkManager.ConnectAsync();
            _cameraUIManager.Setup(_playerManager);
        }

        private bool ValidateSetup()
        {
            if (playerPrefab == null)
            {
                Debug.LogError("Player Prefab is not assigned!", this);
                return false;
            }

            return true;
        }

        void Update()
        {
            _playerManager?.RemoteManager?.InterpolateRemotePlayers(); // Fixed: Use RemoteManager
        }

        void FixedUpdate()
        {
            _networkManager?.DispatchMessageQueue();
        }

        void OnDestroy()
        {
            _networkManager?.Dispose();
            _isPrimaryController = true;
        }
    }
}