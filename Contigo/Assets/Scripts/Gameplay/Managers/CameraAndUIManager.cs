using UnityEngine;
using Gameplay.Managers;
using InkaCamera;

public class CameraAndUIManager : MonoBehaviour
{
    private void Start()
    {
        if (PlayerManager.Instance != null)
        {
            PlayerManager.Instance.OnLocalPlayerSpawned += OnLocalPlayerSpawned;
            Debug.Log("[CameraAndUIManager] Subscribed to OnLocalPlayerSpawned.");
            // Check if player is already spawned
            if (PlayerManager.Instance.LocalPlayer != null)
            {
                OnLocalPlayerSpawned(PlayerManager.Instance.LocalPlayer.GetComponentInChildren<Core.Input.PlayerCharacterInput>());
            }
            else
            {
                Debug.Log("[CameraAndUIManager] LocalPlayer not yet spawned at Start.");
            }
        }
        else
        {
            Debug.LogError("[CameraAndUIManager] PlayerManager.Instance is null at Start!");
        }
    }

    private void OnDestroy()
    {
        if (PlayerManager.Instance != null)
        {
            PlayerManager.Instance.OnLocalPlayerSpawned -= OnLocalPlayerSpawned;
            Debug.Log("[CameraAndUIManager] Unsubscribed from OnLocalPlayerSpawned.");
        }
    }

    private void OnLocalPlayerSpawned(Core.Input.PlayerCharacterInput input)
    {
        var cam = Camera.main;
        if (cam != null)
        {
            var controller = cam.GetComponent<CameraController>();
            if (controller != null && PlayerManager.Instance.LocalPlayer != null)
            {
                controller.playerTransform = PlayerManager.Instance.LocalPlayer.transform;
                Debug.Log($"[CameraAndUIManager] Camera target set to {PlayerManager.Instance.LocalPlayer.name} at position: {controller.playerTransform.position}");
            }
            else
            {
                Debug.LogWarning($"[CameraAndUIManager] Failed to set camera target. Controller: {(controller == null ? "null" : "exists")}, LocalPlayer: {(PlayerManager.Instance.LocalPlayer == null ? "null" : "exists")}");
            }
        }
        else
        {
            Debug.LogWarning("[CameraAndUIManager] Main camera not found.");
        }
    }
}