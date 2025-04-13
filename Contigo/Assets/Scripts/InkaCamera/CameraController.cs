using UnityEngine;
using Gameplay.Managers;

namespace InkaCamera
{
    public class CameraController : MonoBehaviour
    {
        [Header("Camera Settings")]
        public float distanceBehind = 5f;
        public float heightOffset = 3f;
        public float smoothTime = 0.3f;
        public float rotationSpeed = 10f;
        public float lookAheadDistance = 5f;
        public float lookUpOffset = 1f;

        public Transform playerTransform;
        private Vector3 velocity = Vector3.zero;
        private bool hasSubscribed = false;

        private void Awake()
        {
            // Ensure only one CameraController exists, prioritize the one on MainCamera
            CameraController[] controllers = FindObjectsByType<CameraController>(FindObjectsSortMode.None);
            if (controllers.Length > 1)
            {
                bool isMainCamera = gameObject.CompareTag("MainCamera");
                foreach (var controller in controllers)
                {
                    if (controller != this)
                    {
                        if (isMainCamera && !controller.gameObject.CompareTag("MainCamera"))
                        {
                            Debug.Log($"[CameraController] Destroying duplicate CameraController on {controller.gameObject.name}, keeping MainCamera.");
                            Destroy(controller);
                        }
                        else if (!isMainCamera)
                        {
                            Debug.Log($"[CameraController] Destroying this CameraController on {gameObject.name}, another exists on {controller.gameObject.name}.");
                            Destroy(this);
                            return;
                        }
                    }
                }
            }

            // Start with the component disabled to prevent LateUpdate until player is assigned
            enabled = false;
            Debug.Log($"[CameraController] Awake called on {gameObject.name}. Component enabled: {enabled}, GameObject active: {gameObject.activeInHierarchy}.");
            TrySubscribe();
        }

        private void Start()
        {
            if (!hasSubscribed)
            {
                TrySubscribe();
            }
        }

        private void TrySubscribe()
        {
            if (PlayerManager.Instance != null)
            {
                PlayerManager.Instance.OnLocalPlayerSpawned += OnLocalPlayerSpawned;
                hasSubscribed = true;
                Debug.Log($"[CameraController] Subscribed to OnLocalPlayerSpawned on {gameObject.name}.");
                if (PlayerManager.Instance.LocalPlayer != null)
                {
                    playerTransform = PlayerManager.Instance.LocalPlayer.transform;
                    enabled = true;
                    Debug.Log($"[CameraController] Player assigned during initialization at position: {playerTransform.position} on {gameObject.name}.");
                }
            }
            else
            {
                Debug.LogError($"[CameraController] PlayerManager.Instance is null during initialization on {gameObject.name}. Cannot subscribe to OnLocalPlayerSpawned.");
            }
        }

        private void OnDestroy()
        {
            if (PlayerManager.Instance != null && hasSubscribed)
            {
                PlayerManager.Instance.OnLocalPlayerSpawned -= OnLocalPlayerSpawned;
                Debug.Log($"[CameraController] Unsubscribed from OnLocalPlayerSpawned on {gameObject.name}.");
            }
        }

        private void OnLocalPlayerSpawned(Core.Input.PlayerCharacterInput input)
        {
            if (PlayerManager.Instance.LocalPlayer != null)
            {
                playerTransform = PlayerManager.Instance.LocalPlayer.transform;
                enabled = true;
                Debug.Log($"[CameraController] Player assigned via OnLocalPlayerSpawned at position: {playerTransform.position} on {gameObject.name}.");
            }
            else
            {
                Debug.LogWarning($"[CameraController] LocalPlayer is null in OnLocalPlayerSpawned on {gameObject.name}!");
            }
        }

        private void LateUpdate()
        {
            if (playerTransform == null)
            {
                Debug.LogWarning($"[CameraController] PlayerTransform is null in LateUpdate on {gameObject.name}. Disabling component.");
                enabled = false;
                return;
            }

            Vector3 playerWorldPosition = playerTransform.position;
            Vector3 relativePosition = new Vector3(0, heightOffset, -distanceBehind);
            Vector3 desiredPos = playerWorldPosition + playerTransform.TransformDirection(relativePosition);
            transform.position = Vector3.SmoothDamp(transform.position, desiredPos, ref velocity, smoothTime);

            Vector3 lookAtTarget = playerWorldPosition + playerTransform.forward * lookAheadDistance + Vector3.up * lookUpOffset;
            Quaternion desiredRotation = Quaternion.LookRotation(lookAtTarget - transform.position);
            transform.rotation = Quaternion.Slerp(transform.rotation, desiredRotation, rotationSpeed * Time.deltaTime);

            Debug.Log($"[CameraController] Camera updated to position: {transform.position} on {gameObject.name}.");
        }
    }
}