using UnityEngine;

namespace Controllers
{
    public class CameraController : MonoBehaviour
    {
        public Transform target; // Set to the local player’s transform
        public Vector3 offset = new Vector3(0, 5, -5); // Adjusted offset to match original
        public float smoothSpeed = 5f;

        public void UpdateCamera()
        {
            if (target == null) return;
            Vector3 desiredPos = target.position + offset;
            float t = 1 - Mathf.Exp(-Time.deltaTime * smoothSpeed); // Match original smoothing
            transform.position = Vector3.Lerp(transform.position, desiredPos, t);
            transform.LookAt(target);
        }
    }
}