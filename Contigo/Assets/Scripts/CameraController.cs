// Assets/Scripts/ClientSystems/CameraController.cs
using UnityEngine;

public class CameraController : MonoBehaviour
{
    public Transform target;  // Set this to the player transform.
    public Vector3 offset = new Vector3(0, 10, -10);
    public float smoothSpeed = 5f;

    void LateUpdate()
    {
        if (target == null) return;
        Vector3 desiredPos = target.position + offset;
        transform.position = Vector3.Lerp(transform.position, desiredPos, smoothSpeed * Time.deltaTime);
        transform.LookAt(target);
    }
}