using UnityEngine;
using UnityEngine.EventSystems;  // For UI checks
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

namespace Core.Input
{
    public class PlayerCharacterInput : MonoBehaviour
    {
        [Header("Character Input Values")]
        public Vector2 move;
        public Vector2 look;
        public bool jump;
        public bool sprint;

        [Header("Movement Settings")]
        public bool analogMovement;

        [Header("Mouse Cursor Settings")]
        public bool cursorLocked = true;
        public bool cursorInputForLook = true;

#if ENABLE_INPUT_SYSTEM
        private PlayerInput _playerInput;

        private void Awake()
        {
            _playerInput = GetComponent<PlayerInput>();
            if (_playerInput == null)
            {
                Debug.LogError("[PlayerCharacterInput] PlayerInput component not found on this GameObject!");
            }
            else
            {
                Debug.Log("[PlayerCharacterInput] PlayerInput component found. Checking Sprint action...");
                var sprintAction = _playerInput.actions?.FindAction("Sprint");
                if (sprintAction != null)
                {
                    Debug.Log("[PlayerCharacterInput] Sprint action found: " + sprintAction.bindings[0].path);
                }
                else
                {
                    Debug.LogError("[PlayerCharacterInput] Sprint action not found in PlayerInput actions!");
                }
            }
        }

        private void Update()
        {
            // Continuously poll the move value from the Input Action every frame.
            if (_playerInput != null)
            {
                var moveAction = _playerInput.actions?.FindAction("Move");
                if (moveAction != null)
                {
                    Vector2 moveValue = moveAction.ReadValue<Vector2>();
                    MoveInput(moveValue);
                    Debug.Log($"[PlayerCharacterInput] Polling move: value={moveValue}");
                }
                else
                {
                    Debug.LogWarning("[PlayerCharacterInput] Move action not found during Update!");
                }

                // Poll for sprint value
                var sprintAction = _playerInput.actions?.FindAction("Sprint");
                if (sprintAction != null)
                {
                    bool isSprintPressed = sprintAction.IsPressed();
                    if (isSprintPressed != sprint)
                    {
                        Debug.Log($"[PlayerCharacterInput] Polling sprint: isPressed={isSprintPressed}");
                        SprintInput(isSprintPressed);
                    }
                }
                else
                {
                    Debug.LogWarning("[PlayerCharacterInput] Sprint action not found during Update!");
                }
            }

            // Use the new Input System to detect mouse clicks.
            if (Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame)
            {
                // Check using pointer ID -1 for the mouse.
                if (EventSystem.current != null && EventSystem.current.IsPointerOverGameObject(-1))
                {
                    SetCursorState(false);
                    Debug.Log("[PlayerCharacterInput] Mouse clicked on UI element, cursor unlocked.");
                }
                else
                {
                    SetCursorState(cursorLocked);
                    Debug.Log("[PlayerCharacterInput] Mouse clicked outside UI, cursor set to: " + cursorLocked);
                }
            }
        }

        public void OnMove(InputValue value)
        {
            // This callback may be used by UI or other systems, but we are also polling the value every frame.
            Debug.Log("[PlayerCharacterInput] OnMove called: " + value.Get<Vector2>());
            MoveInput(value.Get<Vector2>());
        }

        public void OnLook(InputValue value)
        {
            if (cursorInputForLook)
            {
                Debug.Log("[PlayerCharacterInput] OnLook called: " + value.Get<Vector2>());
                LookInput(value.Get<Vector2>());
            }
        }

        public void OnJump(InputValue value)
        {
            bool isPressed = value.Get<float>() > 0;
            Debug.Log($"[PlayerCharacterInput] OnJump called: isPressed={isPressed}");
            JumpInput(isPressed);
        }

        public void OnSprint(InputValue value)
        {
            bool isPressed = value.Get<float>() > 0;
            Debug.Log($"[PlayerCharacterInput] OnSprint called: isPressed={isPressed}");
            SprintInput(isPressed);
        }
#endif

        public void MoveInput(Vector2 newMoveDirection)
        {
            move = newMoveDirection;
        }

        public void LookInput(Vector2 newLookDirection)
        {
            look = newLookDirection;
        }

        public void JumpInput(bool newJumpState)
        {
            jump = newJumpState;
        }

        public void SprintInput(bool newSprintState)
        {
            sprint = newSprintState;
            Debug.Log($"[PlayerCharacterInput] Sprint state set to: {sprint}");
        }

        private void OnApplicationFocus(bool hasFocus)
        {
            // Do not force the cursor state on focus change.
            Debug.Log($"[PlayerCharacterInput] Application focus changed: hasFocus={hasFocus}");
        }

        private void SetCursorState(bool newState)
        {
            Cursor.lockState = newState ? CursorLockMode.Locked : CursorLockMode.None;
            Cursor.visible = !newState;  // Hide cursor when locked; show when unlocked.
        }
    }
}
