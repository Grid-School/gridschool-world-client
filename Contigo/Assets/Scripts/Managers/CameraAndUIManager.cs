using UnityEngine;
using StarterAssets;

namespace Managers
{
    public class CameraAndUIManager
    {
        public void Setup(PlayerManager playerManager)
        {
            var mainCamera = Camera.main;
            if (mainCamera != null)
            {
                var cameraController = mainCamera.GetComponent<Controllers.CameraController>();
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
                var inputs = playerManager.LocalPlayer?.GetComponent<StarterAssetsInputs>();
                if (uiController != null && inputs != null)
                {
                    uiController.starterAssetsInputs = inputs;
                    Debug.Log("Connected UI canvas to StarterAssetsInputs");
                }
            }
        }
    }
}