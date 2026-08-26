using Core.Data.ClientPlayerData;
using Core.Initialization;
using Core.Input;
using Core.Networking;
using Gameplay.Managers;
using UnityEngine;
using TMPro;
using UnityEngine.InputSystem;

public class ChatInputManager : MonoBehaviour
{
    [SerializeField] private TMP_InputField chatInputField;
    [SerializeField] private PlayerManager playerManager; // Keep this for Inspector assignment if needed
    private ChatBubble localChatBubble;
    private bool isChatMode = false;

    void Start()
    {
        // If playerManager is not assigned in the Inspector, find it dynamically
        if (playerManager == null)
        {
            GameObject initializerObject = GameObject.Find("GameInitializer"); // Adjust name if different
            if (initializerObject != null)
            {
                GameInitializer initializer = initializerObject.GetComponent<GameInitializer>();
                if (initializer != null)
                {
                    playerManager = initializer.PlayerManager; // Access the PlayerManager property
                    Debug.Log("[ChatInputManager] PlayerManager assigned dynamically via GameInitializer.");
                }
                else
                {
                    Debug.LogError("[ChatInputManager] GameInitializer component not found on GameInitializer object!");
                }
            }
            else
            {
                Debug.LogError("[ChatInputManager] GameInitializer object not found in the scene!");
            }
        }

        if (playerManager == null)
        {
            Debug.LogError("[ChatInputManager] PlayerManager could not be assigned!");
            return;
        }

        // Hide input field by default
        chatInputField.gameObject.SetActive(false);

        // Subscribe to local player spawn event
        playerManager.OnLocalPlayerSpawned += OnLocalPlayerSpawned;

        // Link input field events
        chatInputField.onValueChanged.AddListener(OnChatTextChanged);
        chatInputField.onSubmit.AddListener(OnChatSubmitted);
    }

    void Update()
    {
        // Toggle chat mode with 'T' key
        if (Keyboard.current.tKey.wasPressedThisFrame)
        {
            ToggleChatMode();
        }

        // Exit chat mode with Escape
        if (isChatMode && Keyboard.current.escapeKey.wasPressedThisFrame)
        {
            ExitChatMode();
        }
    }

    private void ToggleChatMode()
    {
        isChatMode = !isChatMode;
        chatInputField.gameObject.SetActive(isChatMode);
        if (isChatMode)
        {
            chatInputField.ActivateInputField();
            // Optionally disable player movement here (e.g., via PlayerController)
        }
        else
        {
            chatInputField.DeactivateInputField();
            // Re-enable player movement here
        }
    }

    private void ExitChatMode()
    {
        isChatMode = false;
        chatInputField.DeactivateInputField();
        chatInputField.gameObject.SetActive(false);
        // Re-enable player movement here
    }

    private void OnLocalPlayerSpawned(PlayerCharacterInput input)
    {
        localChatBubble = input.gameObject.GetComponentInChildren<ChatBubble>();
    }

    private void OnChatTextChanged(string text)
    {
        if (localChatBubble != null)
        {
            localChatBubble.SetText(text); // Update local chat bubble in real-time
        }
    }

    private void OnChatSubmitted(string text)
    {
        if (!string.IsNullOrEmpty(text))
        {
            SendChatMessage(text);
            chatInputField.text = ""; // Clear input field after submission
            chatInputField.ActivateInputField(); // Keep input active for next message
        }
    }

    private void SendChatMessage(string message)
    {
        // Assuming InkaNetworkManager handles sending InputMessage
        InputMessage inputMessage = new InputMessage
        {
            X = 0,
            Y = 0,
            Z = 0,
            Angle = 0,
            Speed = 0,
            MotionSpeed = 0,
            Jump = false,
            Grounded = true,
            FreeFall = false,
            ChatMessage = message
        };
        InkaNetworkManager.Instance.SendInputMessage(inputMessage);
    }

    void OnDestroy()
    {
        if (playerManager != null)
        {
            playerManager.OnLocalPlayerSpawned -= OnLocalPlayerSpawned;
        }
        chatInputField.onValueChanged.RemoveListener(OnChatTextChanged);
        chatInputField.onSubmit.RemoveListener(OnChatSubmitted);
    }
}