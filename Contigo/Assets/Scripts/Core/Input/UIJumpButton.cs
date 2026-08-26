using Core.Input;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class UIJumpButton : MonoBehaviour, IPointerDownHandler, IPointerUpHandler
{
    public UICanvasControllerInput inputBridge;

    void Start()
    {
        GetComponent<Button>().onClick.AddListener(() => Debug.Log("[TestButton] Button clicked!"));
    }
    
    public void OnPointerDown(PointerEventData eventData)
    {
        inputBridge?.VirtualJumpInput(true);
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        inputBridge?.VirtualJumpInput(false);
    }
}