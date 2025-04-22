// using Gameplay.InkaCamera;
// using UnityEngine;
// using Gameplay.Managers;
//
// public class CameraAndUIManager : MonoBehaviour
// {
//     private void Start()
//     {
//         if (PlayerManager.Instance != null)
//         {
//             PlayerManager.Instance.OnLocalPlayerSpawned += OnLocalPlayerSpawned;
//             Debug.Log("[CameraAndUIManager] Subscribed to OnLocalPlayerSpawned.");
//             // Check if player is already spawned
//             if (PlayerManager.Instance.LocalPlayer != null)
//             {
//                 OnLocalPlayerSpawned(PlayerManager.Instance.LocalPlayer.GetComponentInChildren<Core.Input.PlayerCharacterInput>());
//             }
//             else
//             {
//                 Debug.Log("[CameraAndUIManager] LocalPlayer not yet spawned at Start.");
//             }
//         }
//         else
//         {
//             Debug.LogError("[CameraAndUIManager] PlayerManager.Instance is null at Start!");
//         }
//     }
//
//     private void OnDestroy()
//     {
//         if (PlayerManager.Instance != null)
//         {
//             PlayerManager.Instance.OnLocalPlayerSpawned -= OnLocalPlayerSpawned;
//             Debug.Log("[CameraAndUIManager] Unsubscribed from OnLocalPlayerSpawned.");
//         }
//     }
//
//     private void OnLocalPlayerSpawned(Core.Input.PlayerCharacterInput input)
//     {
//         var cam = Camera.main;
//         if (cam != null)
//         {
//             var controller = cam.GetComponent<CameraController>();
//             if (controller != null && PlayerManager.Instance.LocalPlayer != null)
//             {
//                 controller.playerTransform = PlayerManager.Instance.LocalPlayer.transform;
//                 Debug.Log($"[CameraAndUIManager] Camera target set to {PlayerManager.Instance.LocalPlayer.name} at position: {controller.playerTransform.position}");
//             }
//             else
//             {
//                 Debug.LogWarning($"[CameraAndUIManager] Failed to set camera target. Controller: {(controller == null ? "null" : "exists")}, LocalPlayer: {(PlayerManager.Instance.LocalPlayer == null ? "null" : "exists")}");
//             }
//         }
//         else
//         {
//             Debug.LogWarning("[CameraAndUIManager] Main camera not found.");
//         }
//     }
// }

// using UnityEngine;
// using InkaCamera;
//
// namespace Gameplay.Managers
// {
//     public class CameraAndUIManager : MonoBehaviour
//     {
//         private PlayerManager _playerManager;
//         private bool _hasSubscribed = false;
//
//         public void Initialize(PlayerManager playerManager)
//         {
//             _playerManager = playerManager;
//             TrySubscribe();
//             Debug.Log($"[CameraAndUIManager] Initialized on {gameObject.name}.");
//         }
//
//         private void TrySubscribe()
//         {
//             if (_playerManager != null)
//             {
//                 _playerManager.OnLocalPlayerSpawned += OnLocalPlayerSpawned;
//                 _hasSubscribed = true;
//                 Debug.Log("[CameraAndUIManager] Subscribed to OnLocalPlayerSpawned.");
//
//                 if (_playerManager.LocalPlayer != null)
//                 {
//                     OnLocalPlayerSpawned(_playerManager.LocalPlayer.GetComponentInChildren<Core.Input.PlayerCharacterInput>());
//                 }
//                 else
//                 {
//                     Debug.Log("[CameraAndUIManager] LocalPlayer not yet spawned at initialization.");
//                 }
//             }
//             else
//             {
//                 Debug.LogError("[CameraAndUIManager] PlayerManager is null during initialization!");
//             }
//         }
//
//         private void OnDestroy()
//         {
//             if (_playerManager != null && _hasSubscribed)
//             {
//                 _playerManager.OnLocalPlayerSpawned -= OnLocalPlayerSpawned;
//                 Debug.Log("[CameraAndUIManager] Unsubscribed from OnLocalPlayerSpawned.");
//             }
//         }
//
//         private void OnLocalPlayerSpawned(Core.Input.PlayerCharacterInput input)
//         {
//             var cam = Camera.main;
//             if (cam != null)
//             {
//                 var controller = cam.GetComponent<CameraController>();
//                 if (controller != null && _playerManager.LocalPlayer != null)
//                 {
//                     Debug.Log($"[CameraAndUIManager] Attempting to set camera target to {_playerManager.LocalPlayer.name}.");
//                     controller.SetPlayerTransform(_playerManager.LocalPlayer.transform);
//                     controller.enabled = true;
//                     Debug.Log($"[CameraAndUIManager] Camera target set to {_playerManager.LocalPlayer.name} at position: {controller.playerTransform.position}");
//                 }
//                 else
//                 {
//                     Debug.LogWarning($"[CameraAndUIManager] Failed to set camera target. Controller: {(controller == null ? "null" : "exists")}, LocalPlayer: {(_playerManager.LocalPlayer == null ? "null" : "exists")}");
//                 }
//             }
//             else
//             {
//                 Debug.LogWarning("[CameraAndUIManager] Main camera not found.");
//             }
//         }
//     }
// }