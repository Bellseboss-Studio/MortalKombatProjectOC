using UnityEngine;

namespace MK.Rebuild
{
    /// <summary>
    /// Placeholder fire breath for the Orochi: an emissive cone and a point light anchored to
    /// the jaw, shown ONLY while the roar clip plays.
    ///
    /// It reads the existing <see cref="CharacterVisual.CurrentState"/> seam, so the boss needs
    /// no new state, no damage path and no gameplay logic. Visual only.
    /// </summary>
    public sealed class FireBreathVisual : MonoBehaviour
    {
        private const string RoarState = "Roar";

        [Tooltip("Boss visual whose current state gates the breath. Defaults to the parent.")]
        [SerializeField] private CharacterVisual visual;

        [Tooltip("Emissive cone root. Toggled on only during the roar.")]
        [SerializeField] private GameObject breathRoot;

        [Tooltip("Point light at the mouth. Its intensity pulses while the breath is on.")]
        [SerializeField] private Light glow;

        [Tooltip("Pulses per second of the light intensity.")]
        [SerializeField] private float pulseSpeed = 14f;

        [SerializeField] private float minIntensity = 1.5f;
        [SerializeField] private float maxIntensity = 5f;

        private void Awake()
        {
            if (visual == null) visual = GetComponentInParent<CharacterVisual>();
            Apply(false, 0f);
        }

        private void Update()
        {
            bool roaring = visual != null && visual.CurrentState == RoarState;
            float pulse = roaring ? Mathf.PingPong(Time.time * pulseSpeed, 1f) : 0f;
            Apply(roaring, pulse);
        }

        private void Apply(bool roaring, float pulse)
        {
            if (breathRoot != null && breathRoot.activeSelf != roaring) breathRoot.SetActive(roaring);

            if (glow == null) return;

            glow.enabled = roaring;
            if (roaring) glow.intensity = Mathf.Lerp(minIntensity, maxIntensity, pulse);
        }
    }
}
