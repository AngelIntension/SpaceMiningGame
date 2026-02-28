using UnityEngine;

namespace VoidHarvest.Features.Docking.Data
{
    /// <summary>
    /// Designer-tunable VFX configuration for docking sequences.
    /// See Spec 004 US5: Docking Audio & Visual Feedback.
    /// </summary>
    [CreateAssetMenu(menuName = "VoidHarvest/DockingVFXConfig")]
    public class DockingVFXConfig : ScriptableObject
    {
        [Header("Approach")]
        public GameObject AlignmentGuideEffect;
        public float ApproachGlowIntensity = 1.0f;

        [Header("Dock Snap")]
        public GameObject SnapFlashEffect;
        public float SnapFlashDuration = 0.5f;

        [Header("Undock Release")]
        public GameObject UndockReleaseEffect;
    }
}
