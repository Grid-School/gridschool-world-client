using UnityEngine;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
using UnityEngine.EventSystems;
#endif

namespace Core.Input
{
    public class PlayerCharacterInput : MonoBehaviour
    {
        [Header("Character Input Values")]
        public Vector2 move;
        public Vector2 look;
        public bool jump; // <- gets pulsed one frame when jump is triggered
        public bool sprint;

        [Header("Settings")]
        public bool analogMovement;
        public bool cursorLocked = true;
        public bool cursorInputForLook = true;

        private bool _cursorToggle = false;
        private Vector2 _uiMove = Vector2.zero;
        private bool _uiSprintHeld = false;

        private bool _jumpTriggered = false;
        private bool _jumpConsumed = false;

#if ENABLE_INPUT_SYSTEM
        private PlayerInput _playerInput;

        private void Awake()
        {
            _playerInput = GetComponent<PlayerInput>();
        }

        private void Update()
        {
            // === MOVEMENT MERGE ===
            Vector2 keyboardMove = Keyboard.current != null
                ? new Vector2(
                    (Keyboard.current.dKey.isPressed ? 1 : 0) + (Keyboard.current.aKey.isPressed ? -1 : 0),
                    (Keyboard.current.wKey.isPressed ? 1 : 0) + (Keyboard.current.sKey.isPressed ? -1 : 0))
                : Vector2.zero;

            move = _uiMove != Vector2.zero ? _uiMove : keyboardMove;

            // === SPRINT ===
            bool shiftHeld = Keyboard.current != null &&
                             (Keyboard.current.leftShiftKey.isPressed || Keyboard.current.rightShiftKey.isPressed);
            sprint = shiftHeld || _uiSprintHeld;

            // === JUMP — Pulse for one frame ===
            if (_jumpTriggered && !_jumpConsumed)
            {
                jump = true;
                _jumpConsumed = true;
            }
            else
            {
                jump = false;
            }

            // === CURSOR TOGGLE ===
            if (Keyboard.current.escapeKey.wasPressedThisFrame)
            {
                _cursorToggle = !_cursorToggle;
                SetCursorState(_cursorToggle);
            }

            if (Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame)
            {
                if (EventSystem.current != null && EventSystem.current.IsPointerOverGameObject(-1))
                    SetCursorState(false);
                else
                    SetCursorState(cursorLocked);
            }
        }

        // === Public API for UI ===
        public void SetUIMove(Vector2 value) => _uiMove = value;
        public void SetUISprint(bool held) => _uiSprintHeld = held;

        public void SetUIJump(bool pressed)
        {
            if (pressed)
            {
                _jumpTriggered = true;
                _jumpConsumed = false;
            }
        }

        // === Input System (spacebar) ===
        public void OnJump(InputValue value)
        {
            if (value.isPressed)
            {
                _jumpTriggered = true;
                _jumpConsumed = false;
            }
        }

        public void LookInput(Vector2 value) => look = value;

        private void SetCursorState(bool locked)
        {
            Cursor.lockState = locked ? CursorLockMode.Locked : CursorLockMode.None;
            Cursor.visible = !locked;
        }

        private void OnApplicationFocus(bool hasFocus)
        {
            if (hasFocus) SetCursorState(false);
        }
#endif
    }
}
