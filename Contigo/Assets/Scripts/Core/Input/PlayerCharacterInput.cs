
using UnityEngine;
using UnityEngine.InputSystem;

namespace Core.Input
{
    [RequireComponent(typeof(PlayerInput))]
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

        public bool isLocalPlayer = true; // default true for safety

        private PlayerInput _playerInput;

        private void Awake()
        {
            Application.runInBackground = true;
            _playerInput = GetComponent<PlayerInput>();

            if (_playerInput == null)
            {
                // Debug.LogError("[PlayerCharacterInput] No PlayerInput found!");
                enabled = false;
                return;
            }
        }

        private void Start()
        {
            if (isLocalPlayer)
            {
                EnableInput();
            }
            else
            {
                DisableInput();
            }
        }

        private void OnEnable()
        {
            if (isLocalPlayer)
                EnableInput();
        }

        private void OnApplicationFocus(bool hasFocus)
        {
            if (hasFocus && isLocalPlayer)
                EnableInput();
        }

        private void EnableInput()
        {
            if (_playerInput != null)
            {
                _playerInput.enabled = true;
                _playerInput.actions?.Enable();
                _playerInput.SwitchCurrentActionMap("Player");
                //Debug.Log("[PlayerCharacterInput] Input Enabled and Action Map Set.");
            }
        }

        private void DisableInput()
        {
            if (_playerInput != null)
            {
                _playerInput.DeactivateInput();
                _playerInput.actions?.Disable();
                _playerInput.enabled = false;
                //Debug.Log("[PlayerCharacterInput] Input Disabled for remote player.");
            }
        }

        public void OnMove(InputValue value)
        {
            if (isLocalPlayer) MoveInput(value.Get<Vector2>());
        }

        public void OnJump(InputValue value)
        {
            if (isLocalPlayer) JumpInput(value.isPressed);
        }

        public void OnSprint(InputValue value)
        {
            if (isLocalPlayer) SprintInput(value.isPressed);
        }

        public void MoveInput(Vector2 newMoveDirection)
        {
            move = newMoveDirection;
            analogMovement = true;
        }

        public void JumpInput(bool newJumpState)
        {
            jump = newJumpState;
        }

        public void SprintInput(bool newSprintState)
        {
            sprint = newSprintState;
        }

        private void SetCursorState(bool newState)
        {
            Cursor.lockState = newState ? CursorLockMode.Locked : CursorLockMode.None;
        }
    }
}

