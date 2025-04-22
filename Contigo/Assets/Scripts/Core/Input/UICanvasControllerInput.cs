using UnityEngine;
using Gameplay.Managers;

namespace Core.Input
{
    public class UICanvasControllerInput : MonoBehaviour
    {
        [Header("Output")]
        public PlayerCharacterInput virtualInput;

        private PlayerManager _playerManager;
        private bool _hasSubscribed = false;

        // Initialize with dependency injection
        public void Initialize(PlayerManager playerManager)
        {
            _playerManager = playerManager;
            TrySubscribe();
            Debug.Log($"[UICanvasControllerInput] Initialized on {gameObject.name}.");
        }

        private void TrySubscribe()
        {
            if (_playerManager != null)
            {
                _playerManager.OnLocalPlayerSpawned += OnLocalPlayerSpawned;
                _hasSubscribed = true;
                Debug.Log("[UICanvasControllerInput] Subscribed to OnLocalPlayerSpawned.");

                // Check if the player is already spawned
                if (_playerManager.LocalPlayer != null)
                {
                    OnLocalPlayerSpawned(_playerManager.LocalPlayer.GetComponentInChildren<PlayerCharacterInput>());
                }
            }
            else
            {
                Debug.LogError("[UICanvasControllerInput] PlayerManager is null during initialization!");
            }
        }

        private void OnLocalPlayerSpawned(PlayerCharacterInput playerInput)
        {
            virtualInput = playerInput;
        }

        public void VirtualMoveInput(Vector2 virtualMoveDirection)
        {
            if (virtualInput == null)
            {
                return;
            }
            virtualInput.MoveInput(virtualMoveDirection);
        }

        public void VirtualJumpInput(bool virtualJumpState)
        {
            if (virtualInput == null)
            {
                return;
            }
            virtualInput.JumpInput(virtualJumpState);
        }

        public void VirtualSprintInput(bool virtualSprintState)
        {
            if (virtualInput == null)
            {
                return;
            }
            virtualInput.SprintInput(virtualSprintState);
        }

        private void OnDestroy()
        {
            if (_playerManager != null && _hasSubscribed)
            {
                _playerManager.OnLocalPlayerSpawned -= OnLocalPlayerSpawned;
            }
        }
    }
}
