using UnityEngine;
using Core.Input;
using Core.Networking;

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
            //Debug.Log("[PlayerManager] Initialized.");
        }

        public void SpawnLocalPlayer(string id)
        {
            //Debug.Log($"[PlayerManager] SpawnLocalPlayer called with id: {id}");
            if (_playerPrefab == null)
            {
                //Debug.LogError("[PlayerManager] Player prefab is not assigned!");
                return;
            }

            if (_localPlayer != null)
            {
                Debug.LogWarning("[PlayerManager] Local player already exists. Destroying old player before spawning new one.");
                Destroy(_localPlayer);
            }

            Vector3 spawnPosition = CalculateSpawnPosition();

            _localPlayer = Instantiate(_playerPrefab, spawnPosition, Quaternion.identity);
            _localPlayer.name = $"Player_{id}";
            _localPlayerTransform = _localPlayer.transform;

            _localPlayerInput = _localPlayer.GetComponent<PlayerCharacterInput>();
            if (_localPlayerInput == null)
            {
                Debug.LogError("[PlayerManager] Player prefab does not have a PlayerCharacterInput component!");
                return;
            }

            _localPlayerInput.isLocalPlayer = true;

            // Ensure chat bubble is enabled
            var chatBubble = _localPlayer.GetComponentInChildren<ChatBubble>();
            if (chatBubble != null)
            {
                chatBubble.SetText("Test"); // Debug text
                Debug.Log($"[PlayerManager] ChatBubble added to local player {id}");
            }
            else
            {
                Debug.LogError("[PlayerManager] ChatBubble not found on local player!");
            }

            var playerController = _localPlayer.GetComponent<PlayerController>();
            if (playerController == null)
            {
                Debug.LogError("[PlayerManager] Player prefab does not have a PlayerController component!");
                return;
            }

            Debug.Log($"[PlayerManager] Local player spawned at position: {_localPlayerTransform.position}");
            
            OnLocalPlayerSpawned?.Invoke(_localPlayerInput);
        }

        private Vector3 CalculateSpawnPosition()
        {
            if (PlanetManager.Instance == null || PlanetManager.Instance.PlanetCenter == null)
            {
                Debug.LogError("[PlayerManager] PlanetManager or PlanetCenter is missing!");
                return Vector3.up * 10f;
            }

            // Get the planet's center position
            Vector3 planetCenter = PlanetManager.Instance.PlanetCenter.position;

            // Get the planet's radius from the sphere collider
            SphereCollider planetCollider = PlanetManager.Instance.PlanetCenter.GetComponent<SphereCollider>();
            if (planetCollider == null)
            {
                Debug.LogError("[PlayerManager] Planet does not have a SphereCollider!");
                return Vector3.up * 10f;
            }
            float planetRadius = planetCollider.radius * PlanetManager.Instance.PlanetCenter.localScale.x;

            // Get the player's height from the capsule collider
            CapsuleCollider playerCollider = _playerPrefab.GetComponent<CapsuleCollider>();
            if (playerCollider == null)
            {
                Debug.LogError("[PlayerManager] Player prefab does not have a CapsuleCollider!");
                return Vector3.up * 10f;
            }
            float playerHeight = playerCollider.height;

            // Calculate the spawn distance (planet radius + half the player's height to place feet on the surface)
            float spawnDistanceFromCenter = planetRadius + playerHeight * 0.5f;

            // Define the spawn direction (e.g., above the planet along the world Y-axis)
            Vector3 direction = Vector3.up; // You can modify this for random or specific spawn points

            // Calculate the spawn position
            Vector3 spawnPosition = planetCenter + direction * spawnDistanceFromCenter;

            Debug.Log($"[PlayerManager] Calculated spawn position: {spawnPosition}, Planet Radius: {planetRadius}, Player Height: {playerHeight}");
            return spawnPosition;
        }
    }
}