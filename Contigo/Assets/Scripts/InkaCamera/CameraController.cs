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
        private bool hasWarned = false;

        private Vector3 velocity = Vector3.zero;

        private void Start()
        {
            if (PlayerManager.Instance != null)
            {
                PlayerManager.Instance.OnLocalPlayerSpawned += OnLocalPlayerSpawned;
                Debug.Log("[CameraController] Subscribed to OnLocalPlayerSpawned.");
            }
            else
            {
                Debug.LogError("[CameraController] PlayerManager.Instance is null at Start!");
            }
        }

        private void OnDestroy()
        {
            // Unsubscribe to prevent memory leaks
            if (PlayerManager.Instance != null)
            {
                PlayerManager.Instance.OnLocalPlayerSpawned -= OnLocalPlayerSpawned;
                Debug.Log("[CameraController] Unsubscribed from OnLocalPlayerSpawned.");
            }
        }

        private void OnLocalPlayerSpawned(Core.Input.PlayerCharacterInput input)
        {
            playerTransform = input.transform; 
            if (PlayerManager.Instance.LocalPlayer != null)
            {
                playerTransform = PlayerManager.Instance.LocalPlayer.transform;
                Debug.Log($"[CameraController] Assigned playerTransform via OnLocalPlayerSpawned at position: {playerTransform.position}");
            }
            else
            {
                Debug.LogWarning("[CameraController] LocalPlayer is null in OnLocalPlayerSpawned!");
            }
        }

        private void LateUpdate()
        {
            if (playerTransform == null)
            {
                if (!hasWarned)
                {
                    Debug.LogWarning("[CameraController] Player Transform is not assigned. Waiting for spawn...");
                    hasWarned = true;
                }
                return; // Exit early if no player transform
            }
            hasWarned = false;

            Vector3 relativePosition = new Vector3(0, heightOffset, -distanceBehind);
            Vector3 desiredPos = playerTransform.TransformPoint(relativePosition);
            transform.position = Vector3.SmoothDamp(transform.position, desiredPos, ref velocity, smoothTime);

            Vector3 lookAtTarget = playerTransform.position + playerTransform.forward * lookAheadDistance + Vector3.up * lookUpOffset;
            Quaternion desiredRotation = Quaternion.LookRotation(lookAtTarget - transform.position);
            transform.rotation = Quaternion.Slerp(transform.rotation, desiredRotation, rotationSpeed * Time.deltaTime);
        }
    }
}