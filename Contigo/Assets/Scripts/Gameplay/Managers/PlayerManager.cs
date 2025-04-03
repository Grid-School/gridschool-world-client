using System;
using UnityEngine;
using Core.Input;
using Gameplay.Player;

namespace Gameplay.Managers
{
    public class PlayerManager : MonoBehaviour
    {
        public static PlayerManager Instance { get; private set; }
        public event Action<PlayerCharacterInput> OnLocalPlayerSpawned;
        public GameObject playerPrefab;
        public GameObject LocalPlayer { get; private set; }
        public PlayerCharacterInput LocalPlayerInput { get; private set; }
        public string LocalPlayerId { get; private set; } // Added

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
            DontDestroyOnLoad(gameObject);
            Debug.Log("[PlayerManager] Instance created.");
        }

        public void SpawnLocalPlayer(string id)
        {
            Debug.Log($"[PlayerManager] SpawnLocalPlayer called with id: {id}");
            LocalPlayerId = id; // Store the network ID
            if (LocalPlayer != null)
            {
                Debug.LogWarning("[PlayerManager] Local player already exists.");
                return;
            }
            if (playerPrefab == null)
            {
                Debug.LogError("[PlayerManager] Player prefab is not assigned!");
                return;
            }
            LocalPlayer = Instantiate(playerPrefab, new Vector3(0, 1, 0), Quaternion.identity);
            Debug.Log($"[PlayerManager] Local player instantiated at {LocalPlayer.transform.position} with ID: {id}");
            LocalPlayerInput = LocalPlayer.GetComponentInChildren<PlayerCharacterInput>();
            if (LocalPlayerInput != null)
            {
                OnLocalPlayerSpawned?.Invoke(LocalPlayerInput);
                Debug.Log("[PlayerManager] OnLocalPlayerSpawned event fired.");
            }
            else
            {
                Debug.LogError("[PlayerManager] PlayerCharacterInput not found on the local player!");
            }
        }

        private void DumpHierarchy(Transform t, string prefix)
        {
            Debug.Log($"{prefix} {t.name} (active: {t.gameObject.activeSelf})");
            foreach (Transform child in t)
            {
                DumpHierarchy(child, prefix + "  ");
            }
        }
    }
}
