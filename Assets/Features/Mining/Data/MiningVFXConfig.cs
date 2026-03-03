using UnityEngine;

namespace VoidHarvest.Features.Mining.Data
{
    /// <summary>
    /// Configuration for mining beam visual effects.
    /// Beam pulse, impact sparks, and heat shimmer parameters.
    /// </summary>
    [CreateAssetMenu(menuName = "VoidHarvest/Mining/Mining VFX Config", fileName = "MiningVFXConfig")]
    public class MiningVFXConfig : ScriptableObject
    {
        [Header("Beam")]
        [Tooltip("LineRenderer/Trail width in meters.")]
        public float BeamWidth = 0.15f;

        [Tooltip("Pulse animation cycles per second.")]
        public float BeamPulseSpeed = 3.0f;

        [Tooltip("Width oscillation range [0,1].")]
        [Range(0f, 1f)]
        public float BeamPulseAmplitude = 0.3f;

        [Header("Impact Sparks")]
        [Tooltip("Sparks per second at impact point.")]
        public int SparkEmissionRate = 15;

        [Tooltip("Spark particle lifetime in seconds.")]
        public float SparkLifetime = 0.4f;

        [Tooltip("Initial outward velocity m/s.")]
        public float SparkSpeed = 3.0f;

        [Header("Heat Shimmer")]
        [Tooltip("Distortion quad opacity [0,1].")]
        [Range(0f, 1f)]
        public float HeatHazeIntensity = 0.5f;

        [Tooltip("Distortion quad size in meters.")]
        public float HeatHazeScale = 0.3f;
    }
}
