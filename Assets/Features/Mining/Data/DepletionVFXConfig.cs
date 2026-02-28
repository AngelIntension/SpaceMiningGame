using UnityEngine;

namespace VoidHarvest.Features.Mining.Data
{
    /// <summary>
    /// Configuration for asteroid depletion visual effects.
    /// Vein glow, crumble bursts, and fragment explosion parameters.
    /// </summary>
    [CreateAssetMenu(menuName = "VoidHarvest/Mining/DepletionVFXConfig", fileName = "DepletionVFXConfig")]
    public class DepletionVFXConfig : ScriptableObject
    {
        [Header("Vein Glow")]
        [Tooltip("Emission intensity at 0% depletion.")]
        public float VeinGlowMinIntensity = 0.0f;

        [Tooltip("Emission intensity at 100% depletion (kept low — no vein mask texture yet).")]
        public float VeinGlowMaxIntensity = 0.6f;

        [Tooltip("Base emission color (warm).")]
        public Color VeinGlowColor = new Color(1f, 0.8f, 0.4f, 1f);

        [Tooltip("Glow pulse cycles per second.")]
        public float VeinGlowPulseSpeed = 1.5f;

        [Tooltip("Pulse intensity oscillation range [0,1] relative to current intensity.")]
        [Range(0f, 1f)]
        public float VeinGlowPulseAmplitude = 0.15f;

        [Header("Crumble Burst")]
        [Tooltip("Particles at 25% threshold.")]
        public int CrumbleBurstCountBase = 8;

        [Tooltip("Multiplier per threshold tier.")]
        public float CrumbleBurstCountScale = 1.5f;

        [Tooltip("Outward velocity m/s.")]
        public float CrumbleBurstSpeed = 5.0f;

        [Tooltip("Particle lifetime seconds.")]
        public float CrumbleBurstLifetime = 0.5f;

        [Tooltip("Flash intensity ramp seconds.")]
        public float CrumbleFlashDuration = 0.3f;

        [Header("Fragment Explosion")]
        [Tooltip("Fragments on final explosion [8-15].")]
        [Range(8, 15)]
        public int FragmentCount = 12;

        [Tooltip("Outward velocity m/s.")]
        public float FragmentSpeed = 4.0f;

        [Tooltip("Time to fade and disappear.")]
        public float FragmentLifetime = 3.0f;

        [Tooltip("Min/max scale for fragments.")]
        public Vector2 FragmentScaleRange = new Vector2(0.05f, 0.2f);
    }
}
