using UnityEngine;

public class SphereCubeGenerator : MonoBehaviour
{
    [SerializeField] private float planetRadius = 10f; // Radius of the spherical planet
    [SerializeField] private int cubeCount = 50; // Number of cubes to generate
    [SerializeField] private Vector2 cubeSizeRange = new Vector2(0.5f, 2f); // Min and max cube size
    [SerializeField] private Vector2 heightOffsetRange = new Vector2(0f, 1f); // Height offset range above surface
    [SerializeField] private string parentObjectName = "GeneratedCubes"; // Name of parent GameObject

    private GameObject cubeParent; // Parent object for grouping cubes

    // Public method to generate cubes (called by Editor script)
    public void GenerateCubes()
    {
        // Clear existing cubes
        ClearCubes();

        // Create parent object
        cubeParent = new GameObject(parentObjectName);
        cubeParent.transform.SetParent(transform); // Parent to the GameObject with this script for organization

        // Generate cubes
        for (int i = 0; i < cubeCount; i++)
        {
            // Generate random spherical coordinates for uniform distribution
            float theta = Random.Range(0f, 2f * Mathf.PI);
            float phi = Mathf.Acos(Random.Range(-1f, 1f));

            // Convert to Cartesian coordinates
            float radius = planetRadius + Random.Range(heightOffsetRange.x, heightOffsetRange.y);
            float x = radius * Mathf.Sin(phi) * Mathf.Cos(theta);
            float y = radius * Mathf.Sin(phi) * Mathf.Sin(theta);
            float z = radius * Mathf.Cos(phi);

            // Create cube
            GameObject cube = GameObject.CreatePrimitive(PrimitiveType.Cube);
            cube.name = $"Cube_{i + 1}";
            cube.transform.position = new Vector3(x, y, z);

            // Set random size
            float size = Random.Range(cubeSizeRange.x, cubeSizeRange.y);
            cube.transform.localScale = new Vector3(size, size, size);

            // Orient cube to face outward from planet center
            cube.transform.up = (cube.transform.position - Vector3.zero).normalized;

            // Add box collider
            BoxCollider collider = cube.GetComponent<BoxCollider>();
            if (collider == null)
            {
                collider = cube.AddComponent<BoxCollider>();
            }

            // Parent cube to group
            cube.transform.SetParent(cubeParent.transform);
        }
    }

    // Public method to clear all generated cubes
    public void ClearCubes()
    {
        if (cubeParent != null)
        {
            DestroyImmediate(cubeParent);
        }
    }
}