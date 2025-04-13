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

        public void OnMove(InputValue value)
        {
            MoveInput(value.Get<Vector2>());
        }

        public void OnJump(InputValue value)
        {
            JumpInput(value.isPressed);
        }

        public void OnSprint(InputValue value)
        {
            SprintInput(value.isPressed);
        }

        public void MoveInput(Vector2 newMoveDirection)
        {
            move = newMoveDirection;
            Debug.Log($"[PlayerCharacterInput] MoveInput called: move={move}");
        }

        public void JumpInput(bool newJumpState)
        {
            jump = newJumpState;
            Debug.Log($"[PlayerCharacterInput] JumpInput called: jump={jump}");
        }

        public void SprintInput(bool newSprintState)
        {
            sprint = newSprintState;
            Debug.Log($"[PlayerCharacterInput] SprintInput called: sprint={sprint}");
        }

        private void OnApplicationFocus(bool hasFocus)
        {
            SetCursorState(cursorLocked && hasFocus);
        }

        private void SetCursorState(bool newState)
        {
            Cursor.lockState = newState ? CursorLockMode.Locked : CursorLockMode.None;
        }
    }
}