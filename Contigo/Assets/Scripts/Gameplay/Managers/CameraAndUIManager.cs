using Core.Input;
using InkaCamera;
using UnityEngine;
using StarterAssets;

namespace Gameplay.Managers
{
    public class CameraAndUIManager
    {
        public void Setup(PlayerManager playerManager)
        {
            var mainCamera = Camera.main;
            if (mainCamera != null)
            {
                var cameraController = mainCamera.GetComponent<CameraController>();
                if (cameraController != null && playerManager.LocalPlayer != null)
                {
                    cameraController.target = playerManager.LocalPlayer.transform;
                    Debug.Log($"Set CameraController target to {playerManager.LocalPlayer.name}");
                }
            }

            var uiCanvas = GameObject.Find("UI_Canvas_StarterAssetsInputs_Joysticks");
            if (uiCanvas != null)
            {
                var uiController = uiCanvas.GetComponent<UICanvasControllerInput>();
                var inputs = playerManager.LocalPlayer?.GetComponent<PlayerCharacterInput>();
                if (uiController != null && inputs != null)
                {
                    uiController.starterAssetsInputs = inputs;
                    Debug.Log("Connected UI canvas to StarterAssetsInputs");
                }
            }
        }
    }
}