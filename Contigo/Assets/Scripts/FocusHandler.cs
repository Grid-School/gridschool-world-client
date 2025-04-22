using UnityEngine;

public class FocusHandler : MonoBehaviour
{
    private void OnApplicationFocus(bool hasFocus)
    {
        Debug.Log($"[PlayerCharacterInput] Application focus changed: {hasFocus}");
    }

    private void OnApplicationPause(bool pauseStatus)
    {
        Debug.Log($"[FocusHandler] Application paused: {pauseStatus} at time {Time.time}");
    }
}