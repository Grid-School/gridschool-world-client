using UnityEngine;

namespace InkaCamera
{
    public class CameraController : MonoBehaviour
    {
        // Assign the player's transform in the Inspector or via code
        public Transform playerTransform;

        // Camera position settings
        public float distanceBehind = 5f; // Distance behind the player
        public float heightOffset = 3f;   // Height above the player

        // Smoothing parameters
        public float smoothTime = 0.3f;   // Position smoothing time (lower = faster)
        public float rotationSpeed = 10f; // Increase this value for more responsive rotation

        // Look-ahead settings: adjust lookAheadDistance to determine how far in front the camera looks
        public float lookAheadDistance = 5f;  // Now set to 5 units ahead of the player
        public float lookUpOffset = 1f;       // Slight upward offset for a better view

        private Vector3 velocity = Vector3.zero;

        void LateUpdate()
        {
            if (playerTransform == null)
            {
                Debug.LogWarning("Player Transform is not assigned in CameraController.");
                return;
            }

            // Calculate the desired position of the camera behind and above the player
            Vector3 relativePosition = new Vector3(0, heightOffset, -distanceBehind);
            Vector3 desiredPos = playerTransform.TransformPoint(relativePosition);
            transform.position = Vector3.SmoothDamp(transform.position, desiredPos, ref velocity, smoothTime);

            // Calculate a look-ahead target: 5 units in front of the player plus an upward offset
            Vector3 lookAtTarget = playerTransform.position + playerTransform.forward * lookAheadDistance + Vector3.up * lookUpOffset;

            // Compute the desired rotation for the camera to look at the target point
            Vector3 direction = lookAtTarget - transform.position;
            Quaternion desiredRotation = Quaternion.LookRotation(direction);

            // Smoothly rotate the camera towards the desired rotation.
            // Adjusting rotationSpeed affects how quickly the camera catches up.
            transform.rotation = Quaternion.Slerp(transform.rotation, desiredRotation, rotationSpeed * Time.deltaTime);
        }
    }
}
