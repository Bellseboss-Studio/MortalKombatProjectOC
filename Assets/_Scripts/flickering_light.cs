using UnityEngine;

[RequireComponent(typeof(Light))]
public class flickering_light : MonoBehaviour
{
  private Light lightoFlicker;
  [SerializeField, Range(0f, 12f)] private float minIntensity = 6.8f;
  [SerializeField, Range(0f, 12f)] private float maxIntensity = 2.4f;
  [SerializeField, Min(0f)] private float timeBetweenIntensity = 0.1f;

    private float currentTimer;
    private void Awake()
    {
        if (lightoFlicker == null)
        {
        lightoFlicker = GetComponent<Light>();
        }

        ValidateIntensityBounds();
    }

    private void Update()
    {
        currentTimer+=Time.deltaTime;
        if (!(currentTimer >= timeBetweenIntensity)) return;
        lightoFlicker.intensity = Random.Range(minIntensity, maxIntensity);
        currentTimer=0;
    }

    private void ValidateIntensityBounds()
    {
        if (!(minIntensity > maxIntensity))
        {
            return;
        }
        Debug.LogWarning("Min Intensity is greater than max Intensity, Swapping Values!");
        (minIntensity, maxIntensity) = (maxIntensity, minIntensity);
    }
}