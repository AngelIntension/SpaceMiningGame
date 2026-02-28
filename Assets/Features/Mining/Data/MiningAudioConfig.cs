using UnityEngine;

namespace VoidHarvest.Features.Mining.Data
{
    /// <summary>
    /// Configuration for mining spatial audio feedback.
    /// Clip references and volume/pitch parameters for all 6 audio cues.
    /// </summary>
    [CreateAssetMenu(menuName = "VoidHarvest/Mining/MiningAudioConfig", fileName = "MiningAudioConfig")]
    public class MiningAudioConfig : ScriptableObject
    {
        [Header("Laser Hum")]
        [Tooltip("Looping beam sound. If null, procedural placeholder is used.")]
        public AudioClip LaserHumClip;

        [Tooltip("Base volume [0,1].")]
        [Range(0f, 1f)]
        public float LaserHumBaseVolume = 0.6f;

        [Tooltip("Pitch at 0% depletion.")]
        public float LaserHumPitchMin = 0.8f;

        [Tooltip("Pitch at 100% depletion.")]
        public float LaserHumPitchMax = 1.4f;

        [Tooltip("Fade-out seconds on stop.")]
        public float LaserHumFadeOutDuration = 0.3f;

        [Header("Spark Crackle")]
        [Tooltip("Impact sound. If null, procedural placeholder is used.")]
        public AudioClip SparkCrackleClip;

        [Tooltip("Volume [0,1].")]
        [Range(0f, 1f)]
        public float SparkCrackleVolume = 0.4f;

        [Header("Crumble Rumble")]
        [Tooltip("Threshold crossing sound. If null, procedural placeholder is used.")]
        public AudioClip CrumbleRumbleClip;

        [Tooltip("Volume [0,1].")]
        [Range(0f, 1f)]
        public float CrumbleRumbleVolume = 0.7f;

        [Header("Explosion")]
        [Tooltip("Final destruction sound. If null, procedural placeholder is used.")]
        public AudioClip ExplosionClip;

        [Tooltip("Volume [0,1].")]
        [Range(0f, 1f)]
        public float ExplosionVolume = 0.8f;

        [Header("Collection Clink")]
        [Tooltip("Ore chunk arrival sound. If null, procedural placeholder is used.")]
        public AudioClip CollectionClinkClip;

        [Tooltip("Volume [0,1].")]
        [Range(0f, 1f)]
        public float CollectionClinkVolume = 0.3f;

        [Header("Spatial")]
        [Tooltip("3D spatial rolloff distance.")]
        public float MaxAudibleDistance = 100.0f;
    }
}
