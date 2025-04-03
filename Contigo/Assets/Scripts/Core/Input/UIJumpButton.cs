using Core.Input;
using UnityEngine;
using UnityEngine.EventSystems;

public class UIJumpButton : MonoBehaviour, IPointerDownHandler, IPointerUpHandler
{
    public UICanvasControllerInput inputBridge;

    public void OnPointerDown(PointerEventData eventData)
    {
        inputBridge?.VirtualJumpInput(true);
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        inputBridge?.VirtualJumpInput(false);
    }
}