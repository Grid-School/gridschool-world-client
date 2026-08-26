using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class CenterGravity : MonoBehaviour
{
    private Rigidbody _rb;

    void Awake()
    {
        _rb = GetComponent<Rigidbody>();
    }

    void FixedUpdate()
    {
        if (PlanetManager.Instance == null || PlanetManager.Instance.PlanetCenter == null) return;
        Vector3 gravityDirection = (PlanetManager.Instance.PlanetCenter.position - transform.position).normalized;
        _rb.AddForce(gravityDirection * PlanetManager.Instance.GravityForce, ForceMode.Acceleration);
    }
}