using UnityEngine;

namespace Core.Initialization
{
    public class EditorFrameRateFix : MonoBehaviour
    {
        private void Awake()
        {
            // Ensure the game runs at a consistent frame rate in the Editor
#if UNITY_EDITOR
            Application.targetFrameRate = 60; // Match your desired frame rate (default FixedUpdate rate is 50 Hz, so 60 is fine)
            QualitySettings.vSyncCount = 0; // Disable VSync to prevent frame rate locking
            Debug.Log("[EditorFrameRateFix] Set target frame rate to 60 and disabled VSync in Editor.");
#endif
        }
    }
}