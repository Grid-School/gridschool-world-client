using UnityEngine;
using Core.Input;

namespace Gameplay.Managers
{
    public class PlayerManager : MonoBehaviour
    {
        public static PlayerManager Instance { get; private set; }
        public GameObject LocalPlayer { get; private set; }
        public string LocalPlayerId { get; private set; }
        public PlayerCharacterInput LocalPlayerInput { get; private set; }
        public event System.Action<PlayerCharacterInput> OnLocalPlayerSpawned;

        private GameObject playerPrefab;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(this);
                return;
            }
            Instance = this;
            Debug.Log($"[PlayerManager] Instance created. Instance is {gameObject.name} ({GetType()}).");
        }

        public void Initialize(GameObject prefab)
        {
            playerPrefab = prefab;
            Debug.Log($"[PlayerManager] Initialized with player prefab: {(playerPrefab != null ? playerPrefab.name : "null")}");
        }

        public void SpawnLocalPlayer(string id)
        {
            Debug.Log($"[PlayerManager] SpawnLocalPlayer called with id: {id}");
            if (playerPrefab == null)
            {
                Debug.LogError("[PlayerManager] Player prefab is null!");
                return;
            }

            LocalPlayer = Instantiate(playerPrefab, Vector3.zero, Quaternion.identity);
            LocalPlayer.name = $"Player_{id}";
            LocalPlayerId = id; // Store the local player's ID
            Debug.Log($"[PlayerManager] Local player spawned at position: {LocalPlayer.transform.position}");

            LocalPlayerInput = LocalPlayer.GetComponent<PlayerCharacterInput>();
            if (LocalPlayerInput == null)
            {
                Debug.LogError($"[PlayerManager] PlayerCharacterInput component not found on player prefab!");
                return;
            }

            OnLocalPlayerSpawned?.Invoke(LocalPlayerInput);
        }

        private void OnDestroy()
        {
            if (Instance == this)
            {
                Instance = null;
            }
        }
    }
}