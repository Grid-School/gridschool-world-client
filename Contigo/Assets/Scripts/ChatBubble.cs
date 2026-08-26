using UnityEngine;
using TMPro;

public class ChatBubble : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI textField;
    [SerializeField] private Canvas canvas;
    private Camera mainCamera;
    private string currentText = ""; // Track the current text
    private Vector3 localOffset;

    void Start()
    {
        mainCamera = Camera.main;
        if (mainCamera == null)
        {
            Debug.LogError("[ChatBubble] MainCamera not found!");
            return;
        }
        
        if (canvas == null)
        {
            Debug.LogError("[ChatBubble] Canvas not assigned!");
            return;
        }
        
        canvas.renderMode = RenderMode.WorldSpace;
        canvas.worldCamera = mainCamera;
        canvas.planeDistance = 0.5f;
        canvas.sortingOrder = 10;
        
        RectTransform rect = canvas.GetComponent<RectTransform>();
        rect.sizeDelta = new Vector2(2f, 1f);
        
        if (textField == null)
        {
            Debug.LogError("[ChatBubble] TextField not assigned!");
            return;
        }
        
        localOffset = transform.localPosition;
        
        Debug.Log($"[ChatBubble] Initialized - Canvas Active: {canvas.gameObject.activeSelf}, Scale: {canvas.transform.localScale}, Parent Scale: {transform.parent.localScale}, Render Mode: {canvas.renderMode}, Event Camera: {(canvas.worldCamera != null ? canvas.worldCamera.name : "null")}, Plane Distance: {canvas.planeDistance}, Size Delta: {rect.sizeDelta}");
        
        if (string.IsNullOrEmpty(currentText))
        {
            SetText("");
        }
        
        StartCoroutine(DebugSetTextCoroutine());
    }

    private System.Collections.IEnumerator DebugSetTextCoroutine()
    {
        yield return new WaitForSeconds(2f);
        Debug.Log("[ChatBubble] Invoking DebugSetText");
        SetText("Delayed Test");
    }

    void LateUpdate()
    {
        if (mainCamera != null)
        {
            // Get the player's world position
            Vector3 playerPos = transform.parent.position;

            // Calculate the desired world position of the chat bubble (player position + local offset)
            Vector3 targetPos = playerPos + transform.parent.TransformDirection(localOffset);

            // Transform the target position to screen space
            Vector3 screenPos = mainCamera.WorldToScreenPoint(targetPos);

            // Adjust the depth to ensure the chat bubble is at a fixed distance from the camera
            screenPos.z = 5f; // Distance from the camera (adjust as needed)

            // Convert back to world space at the fixed distance
            Vector3 newWorldPos = mainCamera.ScreenToWorldPoint(screenPos);

            // Update the chat bubble's position
            transform.position = newWorldPos;

            // Align the chat bubble's rotation to match the camera's orientation
            transform.rotation = mainCamera.transform.rotation;

            // Debug the positions
        }
    }

    public void SetText(string text)
    {
        currentText = text;

        if (string.IsNullOrEmpty(text) || textField == null)
        {
            if (canvas != null)
            {
                canvas.gameObject.SetActive(false);
            }
            Debug.Log("[ChatBubble] Text empty or TextField null, hiding canvas.");
        }
        else
        {
            if (canvas != null)
            {
                canvas.gameObject.SetActive(true);
                textField.text = text;
                Debug.Log($"[ChatBubble] Set text: {text}, Canvas Active: {canvas.gameObject.activeSelf}, Text Color: {textField.color}, Font Size: {textField.fontSize}, Rect Size: {textField.GetComponent<RectTransform>().sizeDelta}");
            }
        }
    }
}