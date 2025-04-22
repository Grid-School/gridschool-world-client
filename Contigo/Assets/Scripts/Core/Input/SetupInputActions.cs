using UnityEngine;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

namespace Core.Input
{
    public class SetupInputActions : MonoBehaviour
    {
#if ENABLE_INPUT_SYSTEM
        [SerializeField] private InputActionAsset inputActionsAsset; // assign this in the Inspector
        private PlayerInput _playerInput;

        private void Awake()
        {
            _playerInput = GetComponent<PlayerInput>();
            if (_playerInput == null)
            {
                Debug.LogError("[SetupInputActions] PlayerInput component not found on this GameObject!");
                return;
            }

            if (inputActionsAsset == null)
            {
                Debug.LogError("[SetupInputActions] Input Action Asset is not assigned in the Inspector!");
                return;
            }

            _playerInput.actions = Instantiate(inputActionsAsset);

            Debug.Log("[SetupInputActions] Assigned cloned Input Action Asset to PlayerInput.");
        }
#endif
    }
}