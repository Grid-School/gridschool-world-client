// using UnityEngine;
// using Gameplay.Managers;
//
// [DefaultExecutionOrder(-100)] // Run before most other scripts.
// public class Bootstrap : MonoBehaviour
// {
//     [SerializeField] private GameObject playerPrefab;
//     [SerializeField] private GameObject playerManagerPrefab; // A prefab with the PlayerManager component.
//
//     private void Awake()
//     {
//         if (playerPrefab == null)
//         {
//             Debug.LogError("Bootstrap: PlayerPrefab is not assigned!");
//             return;
//         }
//         if (playerManagerPrefab == null)
//         {
//             Debug.LogError("Bootstrap: PlayerManagerPrefab is not assigned!");
//             return;
//         }
//         
//         // Create the PlayerManager early if it does not exist.
//         if (PlayerManager.Instance == null)
//         {
//             // Instantiate the prefab that has the PlayerManager component.
//             Instantiate(playerManagerPrefab);
//             Debug.Log("Bootstrap: PlayerManager instance created.");
//         }
//     }
// }