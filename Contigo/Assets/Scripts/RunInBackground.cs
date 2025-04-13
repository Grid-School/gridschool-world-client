using UnityEngine;

public class RunInBackground : MonoBehaviour
{
    private void Start()
    {
        // Ensure the game continues running in the background
        Application.runInBackground = true;
        Debug.Log("[RunInBackground] Set Application.runInBackground to true.");

#if UNITY_WEBGL && !UNITY_EDITOR
        // Force WebGL to keep running by preventing throttling
        InvokeRepeating(nameof(KeepAlive), 0f, 1f);
#endif
    }

    private void KeepAlive()
    {
        Debug.Log("[RunInBackground] KeepAlive called to prevent WebGL throttling at time " + Time.time);
    }
}