using Core.Input;
using UnityEngine;
using UnityEngine.EventSystems;

public class UISprintButton : MonoBehaviour, IPointerDownHandler, IPointerUpHandler
{
    public UICanvasControllerInput inputBridge;

    public void OnPointerDown(PointerEventData eventData)
    {
        inputBridge?.VirtualSprintInput(true);
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        inputBridge?.VirtualSprintInput(false);
    }
}