using UnityEngine;

namespace Gameplay
{
    /// <summary>
    /// The spawn Beacon flickers while GF-1 (the health-check lie) is unfixed.
    /// Fixing GF-1 earns the right to set steady = true. The world displays its own bugs.
    /// </summary>
    public class BeaconFlicker : MonoBehaviour
    {
        [Tooltip("Set true only when GF-1 is fixed on main.")]
        public bool steady = false;

        [SerializeField] private Light beaconLight;
        [SerializeField] private Renderer coreRenderer;
        [SerializeField] private float baseLightIntensity = 3f;
        [SerializeField] private Color emissionColor = new Color(0.157f, 0.882f, 1f); // #28E1FF
        [SerializeField] private float baseEmission = 4f;

        private MaterialPropertyBlock _block;
        private static readonly int EmissionId = Shader.PropertyToID("_EmissionColor");

        private void Awake()
        {
            _block = new MaterialPropertyBlock();
            if (beaconLight == null) beaconLight = GetComponentInChildren<Light>();
            if (coreRenderer == null) coreRenderer = GetComponent<Renderer>();
        }

        private void Update()
        {
            float level = 1f;
            if (!steady)
            {
                // Perlin gives an organic stutter; the occasional deep dip reads as "unhealthy".
                float n = Mathf.PerlinNoise(Time.time * 2.3f, 0.37f);
                level = Mathf.Lerp(0.15f, 1f, n * n);
            }

            if (beaconLight != null) beaconLight.intensity = baseLightIntensity * level;
            if (coreRenderer != null)
            {
                coreRenderer.GetPropertyBlock(_block);
                _block.SetColor(EmissionId, emissionColor * (baseEmission * level));
                coreRenderer.SetPropertyBlock(_block);
            }
        }
    }
}
