using UnityEngine;

namespace VoidHarvest.Features.Docking.Data
{
    /// <summary>
    /// Designer-tunable audio configuration for docking sequences.
    /// See Spec 004 US5: Docking Audio & Visual Feedback.
    /// </summary>
    [CreateAssetMenu(menuName = "VoidHarvest/DockingAudioConfig")]
    public class DockingAudioConfig : ScriptableObject
    {
        [Header("Approach")]
        public AudioClip ApproachHumClip;

        [Header("Dock")]
        public AudioClip DockClampClip;
        [Range(0f, 1f)]
        public float DockClampVolume = 0.8f;

        [Header("Undock")]
        public AudioClip UndockReleaseClip;
        [Range(0f, 1f)]
        public float UndockReleaseVolume = 0.6f;

        public AudioClip EngineStartClip;

        [Header("Spatial")]
        public float MaxAudibleDistance = 200f;
    }
}
