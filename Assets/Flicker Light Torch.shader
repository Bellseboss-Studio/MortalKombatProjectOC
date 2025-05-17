using UnityEngine;

[RequireComponent(typeof(Light))]
public class TorchFlicker : MonoBehaviour
{
    [SerializeField] private float baseIntensity = 2.0f; // Base light intensity
    [SerializeField] private float maxIntensity = 4.0f; // Maximum intensity during flicker
    [SerializeField] private float flickerSpeed = 5.0f; // Speed of intensity changes
    [SerializeField] private float pulseChance = 0.05f; // Chance per frame for a random pulse (0-1)
    [SerializeField] private float pulseStrength = 1.5f; // Extra intensity for pulses
    [SerializeField] private bool flickerRange = false; // Whether to flicker the light's range
    [SerializeField] private float baseRange = 5.0f; // Base light range
    [SerializeField] private float maxRange = 7.0f; // Maximum range during flicker

    private Light pointLight;
    private float noiseOffset;

    void Start()
    {
        // Get the Point Light component
        pointLight = GetComponent<Light>();
        if (pointLight.type != LightType.Point)
        {
            Debug.LogWarning("TorchFlicker requires a Point Light component.");
            enabled = false;
            return;
        }

        // Initialize random offset for Perlin noise
        noiseOffset = Random.Range(0f, 100f);

        // Set initial values
        pointLight.intensity = baseIntensity;
        pointLight.range = baseRange;
    }

    void Update()
    {
        // Calculate Perlin noise-based intensity
        float noise = Mathf.PerlinNoise(Time.time * flickerSpeed + noiseOffset, 0f);
        float intensity = Mathf.Lerp(baseIntensity, maxIntensity, noise);

        // Add random pulse
        if (Random.value < pulseChance)
        {
            intensity += pulseStrength * (1f - noise); // Stronger pulse when noise is low
        }

        // Clamp intensity to avoid extreme values
        intensity = Mathf.Clamp(intensity, baseIntensity, maxIntensity + pulseStrength);

        // Apply intensity to light
        pointLight.intensity = intensity;

        // Optionally flicker range
        if (flickerRange)
        {
            float range = Mathf.Lerp(baseRange, maxRange, (intensity - baseIntensity) / (maxIntensity - baseIntensity));
            pointLight.range = Mathf.Clamp(range, baseRange, maxRange);
        }
    }
}