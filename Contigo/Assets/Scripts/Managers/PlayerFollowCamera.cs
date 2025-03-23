// using UnityEngine;
//
// namespace Managers
// {
//     public class PlayerFollowCamera : MonoBehaviour
//     {
//         [SerializeField] private Transform target;
//         [SerializeField] private float distance = 4f; // Closer to Starter Assets
//         [SerializeField] private float height = 2f; // Above player
//         [SerializeField] private float smoothSpeed = 0.3f; // Smoother following
//
//         private Vector3 velocity = Vector3.zero;
//
//         private void LateUpdate()
//         {
//             if (target == null) return;
//
//             // Calculate desired position (behind and above player)
//             Vector3 desiredPosition = target.position - (target.forward * distance) + (Vector3.up * height);
//         
//             // Smoothly move camera to desired position
//             transform.position = Vector3.SmoothDamp(transform.position, desiredPosition, ref velocity, smoothSpeed);
//
//             // Look at the player (slightly above for better view)
//             transform.LookAt(target.position + Vector3.up * 1f);
//         }
//
//         public void SetTarget(Transform newTarget)
//         {
//             target = newTarget;
//         }
//     }
// }