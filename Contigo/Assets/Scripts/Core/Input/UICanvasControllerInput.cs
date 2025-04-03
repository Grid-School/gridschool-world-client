using System.Collections;
using UnityEngine;
using Core.Input;
using Gameplay.Managers;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.UI; // new input system

namespace Core.Input
{
    public class UICanvasControllerInput : MonoBehaviour
    {
        public PlayerCharacterInput localPlayerInput;
        private Vector2 lastUIMoveInput = Vector2.zero;
        private bool hasUIJoystickMoved = false;

        private void OnEnable()
        {
            StartCoroutine(WaitForLocalPlayerInput());
        }
        
        private void Start()
        {
            // Safety check: re-enable all InputModules
            var system = EventSystem.current;
            if (system != null && system.currentInputModule is InputSystemUIInputModule inputModule)
            {
                inputModule.enabled = false;
                inputModule.enabled = true;
                Debug.Log("[UICanvasControllerInput] Re-enabled InputSystemUIInputModule");
            }
        }


        private IEnumerator WaitForLocalPlayerInput()
        {
            while (PlayerManager.Instance == null || PlayerManager.Instance.LocalPlayerInput == null)
                yield return null;

            localPlayerInput = PlayerManager.Instance.LocalPlayerInput;
        }

        private void Update()
        {
            // ✅ Fallback: if UI Move input hasn’t been called but input exists
            if (!hasUIJoystickMoved && localPlayerInput != null)
            {
                // This checks for InputAction "Move" in case UI binding is lost
                Vector2 rawMove = Vector2.zero;
                var gamepad = Gamepad.current;
                if (gamepad != null)
                {
                    rawMove = new Vector2(gamepad.leftStick.x.ReadValue(), gamepad.leftStick.y.ReadValue());
                }

                if (rawMove != Vector2.zero)
                {
                    localPlayerInput.SetUIMove(rawMove);
                }
            }
        }

        public void VirtualMoveInput(Vector2 value)
        {
            if (localPlayerInput != null)
            {
                hasUIJoystickMoved = true;
                lastUIMoveInput = value;
                localPlayerInput.SetUIMove(value);
            }
        }

        public void VirtualLookInput(Vector2 value)
        {
            localPlayerInput?.LookInput(value);
        }

        public void VirtualJumpInput(bool value)
        {
            localPlayerInput?.SetUIJump(value);
        }

        public void VirtualSprintInput(bool value)
        {
            localPlayerInput?.SetUISprint(value);
        }
    }
}
