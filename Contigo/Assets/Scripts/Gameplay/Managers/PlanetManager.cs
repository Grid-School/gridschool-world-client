using UnityEngine;

public class PlanetManager : MonoBehaviour
{
    public static PlanetManager Instance { get; private set; }
    public Transform PlanetCenter;
    public float GravityForce = 15.81f;
    public Transform SunLight; // Assign Directional Light in Inspector
    public Material skyboxMaterial; // Assign SkyboxMaterial

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    private void Update()
    {
        if (skyboxMaterial != null && SunLight != null)
        {
            Vector3 sunDir = -SunLight.forward; // Light points opposite to direction
            skyboxMaterial.SetVector("_SunDirection", sunDir.normalized);
        }
    }
}