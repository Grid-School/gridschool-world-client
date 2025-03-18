using UnityEngine;

namespace Controllers
{
    public class CameraController : MonoBehaviour
    {
        public Transform target; // Set to the local player’s transform
        public float heightOffset = 5f; // Vertical distance above the player
        public float distanceBehind = 5f; // Distance behind the player
        public float smoothSpeed = 5f; // Smoothing factor (higher = faster)

        private Vector3 _velocity = Vector3.zero; // For SmoothDamp

        public void UpdateCamera()
        {
            if (target == null) return;

            // Calculate the desired position behind the player based on their rotation
            Vector3 behindDirection = -target.forward; // Negative forward = behind
            Vector3 desiredPos = target.position + behindDirection * distanceBehind + Vector3.up * heightOffset;

            // Smoothly move the camera to the desired position
            transform.position = Vector3.SmoothDamp(transform.position, desiredPos, ref _velocity, 1f / smoothSpeed);

            // Always look at the player
            transform.LookAt(target);
        }
    }
}