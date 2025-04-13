using UnityEngine;

public class FocusHandler : MonoBehaviour
{
    private void OnApplicationFocus(bool hasFocus)
    {
        Debug.Log($"[FocusHandler] Application focus changed: {hasFocus} at time {Time.time}");
    }

    private void OnApplicationPause(bool pauseStatus)
    {
        Debug.Log($"[FocusHandler] Application paused: {pauseStatus} at time {Time.time}");
    }
}