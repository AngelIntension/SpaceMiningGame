using UnityEngine;

namespace VoidHarvest.Features.Mining.Systems
{
    /// <summary>
    /// Pure static formulas for mining VFX calculations.
    /// All methods are testable with no side effects.
    /// </summary>
    public static class MiningVFXFormulas
    {
        /// <summary>
        /// Calculates beam width with sinusoidal pulse.
        /// Formula: baseWidth * (1 + sin(2*PI*speed*time) * amplitude)
        /// </summary>
        public static float CalculatePulseWidth(float baseWidth, float pulseSpeed, float pulseAmplitude, float time)
        {
            float pulse = Mathf.Sin(2f * Mathf.PI * pulseSpeed * time);
            return baseWidth * (1f + pulse * pulseAmplitude);
        }

        /// <summary>
        /// Resolves spark color from ore beam color. Falls back to white.
        /// </summary>
        public static Color ResolveSparkColor(Color? oreBeamColor)
        {
            return oreBeamColor ?? Color.white;
        }

        /// <summary>
        /// Returns heat shimmer opacity clamped to [0,1].
        /// </summary>
        public static float CalculateHeatShimmerOpacity(float configIntensity)
        {
            return Mathf.Clamp01(configIntensity);
        }

        /// <summary>
        /// Calculates emission intensity from depletion using sqrt ease-in curve.
        /// Formula: lerp(min, max, sqrt(depletion))
        /// </summary>
        public static float CalculateEmissionIntensity(float depletion, float minIntensity, float maxIntensity)
        {
            float t = Mathf.Sqrt(Mathf.Clamp01(depletion));
            return Mathf.Lerp(minIntensity, maxIntensity, t);
        }

        /// <summary>
        /// Applies sinusoidal pulse modulation to an intensity value.
        /// Formula: intensity * (1 + sin(2*PI*speed*time) * amplitude)
        /// </summary>
        public static float ApplyPulseModulation(float intensity, float pulseSpeed, float pulseAmplitude, float time)
        {
            float pulse = Mathf.Sin(2f * Mathf.PI * pulseSpeed * time);
            return intensity * (1f + pulse * pulseAmplitude);
        }

        /// <summary>
        /// Interpolates color from ore color to red/orange based on depletion.
        /// </summary>
        public static Color CalculateDepletionColor(Color oreColor, float depletion)
        {
            var depletedColor = new Color(1f, 0.3f, 0.1f, 1f); // Red-orange
            return Color.Lerp(oreColor, depletedColor, depletion);
        }

        /// <summary>
        /// Calculates audio pitch from depletion fraction.
        /// Formula: lerp(pitchMin, pitchMax, depletion)
        /// </summary>
        public static float CalculateHumPitch(float depletion, float pitchMin, float pitchMax)
        {
            return Mathf.Lerp(pitchMin, pitchMax, Mathf.Clamp01(depletion));
        }

        /// <summary>
        /// Calculates fade-out volume over time.
        /// Formula: 1 - (elapsed / duration), clamped to [0,1]
        /// </summary>
        public static float CalculateFadeVolume(float elapsed, float duration)
        {
            if (duration <= 0f) return 0f;
            return Mathf.Clamp01(1f - elapsed / duration);
        }

        /// <summary>
        /// Evaluates a quadratic bezier curve at parameter t.
        /// </summary>
        public static Vector3 EvaluateBezier(Vector3 p0, Vector3 p1, Vector3 p2, float t)
        {
            float u = 1f - t;
            return u * u * p0 + 2f * u * t * p1 + t * t * p2;
        }
    }
}
