using UnityEngine;
using Gameplay.Managers;

namespace Core.Input
{
    public class UICanvasControllerInput : MonoBehaviour
    {
        [Header("Output")]
        public PlayerCharacterInput virtualInput;

        private void Start()
        {
            // Subscribe to PlayerManager's spawn event
            if (PlayerManager.Instance != null)
            {
                PlayerManager.Instance.OnLocalPlayerSpawned += OnLocalPlayerSpawned;
            }
            else
            {
                Debug.LogError("[UICanvasControllerInput] PlayerManager instance not found!");
            }
        }

        private void OnLocalPlayerSpawned(PlayerCharacterInput playerInput)
        {
            virtualInput = playerInput;
            Debug.Log("[UICanvasControllerInput] Assigned virtualInput from spawned player.");
        }

        public void VirtualMoveInput(Vector2 virtualMoveDirection)
        {
            if (virtualInput == null)
            {
                Debug.LogError("[UICanvasControllerInput] virtualInput is not assigned!");
                return;
            }
            virtualInput.MoveInput(virtualMoveDirection);
            Debug.Log($"[UICanvasControllerInput] VirtualMoveInput called: {virtualMoveDirection}");
        }

        public void VirtualJumpInput(bool virtualJumpState)
        {
            if (virtualInput == null)
            {
                Debug.LogError("[UICanvasControllerInput] virtualInput is not assigned!");
                return;
            }
            virtualInput.JumpInput(virtualJumpState);
            Debug.Log($"[UICanvasControllerInput] VirtualJumpInput called: {virtualJumpState}");
        }

        public void VirtualSprintInput(bool virtualSprintState)
        {
            if (virtualInput == null)
            {
                Debug.LogError("[UICanvasControllerInput] virtualInput is not assigned!");
                return;
            }
            virtualInput.SprintInput(virtualSprintState);
            Debug.Log($"[UICanvasControllerInput] VirtualSprintInput called: {virtualSprintState}");
        }

        private void OnDestroy()
        {
            // Unsubscribe from the event to prevent memory leaks
            if (PlayerManager.Instance != null)
            {
                PlayerManager.Instance.OnLocalPlayerSpawned -= OnLocalPlayerSpawned;
            }
        }
    }
}