using UnityEngine;
using UnityEngine.EventSystems;

namespace Core.Input
{
    public class UIVirtualJoystick : MonoBehaviour, IPointerDownHandler, IDragHandler, IPointerUpHandler
    {
        [Header("Joystick References")]
        public RectTransform containerRect; // Background circle
        public RectTransform handleRect;    // Inner handle circle

        [Header("Joystick Settings")]
        public float joystickRange = 90f;
        public float magnitudeMultiplier = 1f;
        public bool invertXOutputValue;
        public bool invertYOutputValue;

        [Header("Hook to Input Handler")]
        public UICanvasControllerInput inputBridge; // Reference to your existing input bridge

        private void Start()
        {
            UpdateHandleRectPosition(Vector2.zero);
        }

        public void OnPointerDown(PointerEventData eventData)
        {
            OnDrag(eventData); // Begin processing drag on press
        }

        public void OnDrag(PointerEventData eventData)
        {
            if (RectTransformUtility.ScreenPointToLocalPointInRectangle(containerRect, eventData.position, eventData.pressEventCamera, out Vector2 position))
            {
                position = ApplySizeDelta(position);
                Vector2 clamped = ClampToUnitCircle(position);
                Vector2 output = ApplyInversion(clamped) * magnitudeMultiplier;

                inputBridge?.VirtualMoveInput(output);
                UpdateHandleRectPosition(clamped * joystickRange);
            }
        }

        public void OnPointerUp(PointerEventData eventData)
        {
            inputBridge?.VirtualMoveInput(Vector2.zero); // Let go = stop movement
            UpdateHandleRectPosition(Vector2.zero);
        }

        private Vector2 ApplySizeDelta(Vector2 position)
        {
            float x = (position.x / containerRect.sizeDelta.x) * 2.5f;
            float y = (position.y / containerRect.sizeDelta.y) * 2.5f;
            return new Vector2(x, y);
        }

        private Vector2 ClampToUnitCircle(Vector2 position)
        {
            return Vector2.ClampMagnitude(position, 1f);
        }

        private Vector2 ApplyInversion(Vector2 position)
        {
            if (invertXOutputValue) position.x = -position.x;
            if (invertYOutputValue) position.y = -position.y;
            return position;
        }

        private void UpdateHandleRectPosition(Vector2 newPos)
        {
            if (handleRect != null)
                handleRect.anchoredPosition = newPos;
        }
    }
}
