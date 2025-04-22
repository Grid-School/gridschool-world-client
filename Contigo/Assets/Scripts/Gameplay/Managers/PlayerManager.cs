// using UnityEngine;
// using Core.Input;
//
// namespace Gameplay.Managers
// {
//     public class PlayerManager : MonoBehaviour
//     {
//         public static PlayerManager Instance { get; private set; }
//         public GameObject LocalPlayer { get; private set; }
//         public string LocalPlayerId { get; private set; }
//         public PlayerCharacterInput LocalPlayerInput { get; private set; }
//         public event System.Action<PlayerCharacterInput> OnLocalPlayerSpawned;
//
//         private GameObject playerPrefab;
//
//         private void Awake()
//         {
//             if (Instance != null && Instance != this)
//             {
//                 Destroy(this);
//                 return;
//             }
//             Instance = this;
//             Debug.Log($"[PlayerManager] Instance created. Instance is {gameObject.name} ({GetType()}).");
//         }
//
//         public void Initialize(GameObject prefab)
//         {
//             playerPrefab = prefab;
//             Debug.Log($"[PlayerManager] Initialized with player prefab: {(playerPrefab != null ? playerPrefab.name : "null")}");
//         }
//
//         public void SpawnLocalPlayer(string id)
//         {
//             Debug.Log($"[PlayerManager] SpawnLocalPlayer called with id: {id}");
//             if (playerPrefab == null)
//             {
//                 Debug.LogError("[PlayerManager] Player prefab is null!");
//                 return;
//             }
//
//             LocalPlayer = Instantiate(playerPrefab, Vector3.zero, Quaternion.identity);
//             LocalPlayer.name = $"Player_{id}";
//             LocalPlayerId = id; // Store the local player's ID
//             Debug.Log($"[PlayerManager] Local player spawned at position: {LocalPlayer.transform.position}");
//
//             LocalPlayerInput = LocalPlayer.GetComponent<PlayerCharacterInput>();
//             if (LocalPlayerInput == null)
//             {
//                 Debug.LogError($"[PlayerManager] PlayerCharacterInput component not found on player prefab!");
//                 return;
//             }
//
//             OnLocalPlayerSpawned?.Invoke(LocalPlayerInput);
//         }
//
//         private void OnDestroy()
//         {
//             if (Instance == this)
//             {
//                 Instance = null;
//             }
//         }
//     }
// }

using Core.Input;
using UnityEngine;
using Core.Networking;
using Gameplay.Player;

namespace Gameplay.Managers
{
    public class PlayerManager : MonoBehaviour
    {
        private GameObject _playerPrefab;
        private InkaNetworkManager _networkManager;
        private GameObject _localPlayer;
        private Transform _localPlayerTransform;
        private PlayerCharacterInput _localPlayerInput;

        public GameObject LocalPlayer => _localPlayer;
        public Transform LocalPlayerTransform => _localPlayerTransform;
        public PlayerCharacterInput LocalPlayerInput => _localPlayerInput;

        public event System.Action<PlayerCharacterInput> OnLocalPlayerSpawned;

        public void Initialize(GameObject playerPrefab, InkaNetworkManager networkManager)
        {
            _playerPrefab = playerPrefab;
            _networkManager = networkManager;
            Debug.Log("[PlayerManager] Initialized.");
        }

        public void SpawnLocalPlayer(string id)
        {
            Debug.Log($"[PlayerManager] SpawnLocalPlayer called with id: {id}");
            if (_playerPrefab == null)
            {
                Debug.LogError("[PlayerManager] Player prefab is not assigned!");
                return;
            }

            if (_localPlayer != null)
            {
                Debug.LogWarning("[PlayerManager] Local player already exists. Destroying old player before spawning new one.");
                Destroy(_localPlayer);
            }
            
            _localPlayer = Instantiate(_playerPrefab, Vector3.zero, Quaternion.identity);
            _localPlayer.name = $"Player_{id}";
            _localPlayerTransform = _localPlayer.transform;

            _localPlayerInput = _localPlayer.GetComponent<PlayerCharacterInput>();
            if (_localPlayerInput == null)
            {
                Debug.LogError("[PlayerManager] Player prefab does not have a PlayerCharacterInput component!");
                return;
            }
            
            _localPlayerInput.isLocalPlayer = true;

            var playerController = _localPlayer.GetComponent<PlayerController>();
            if (playerController == null)
            {
                Debug.LogError("[PlayerManager] Player prefab does not have a PlayerController component!");
                return;
            }
            playerController.Initialize(_networkManager, _localPlayerInput);

            Debug.Log($"[PlayerManager] Local player spawned at position: {_localPlayerTransform.position}");
            OnLocalPlayerSpawned?.Invoke(_localPlayerInput);
        }
    }
}
