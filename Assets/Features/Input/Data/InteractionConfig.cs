using UnityEngine;

namespace VoidHarvest.Features.Input.Data
{
    /// <summary>
    /// Designer-tunable input and interaction timing parameters.
    /// See Spec 009: Data-Driven World Config (US5).
    /// </summary>
    [CreateAssetMenu(fileName = "InteractionConfig", menuName = "VoidHarvest/Input/Interaction Config")]
    public class InteractionConfig : ScriptableObject
    {
        [Tooltip("Time window for double-click detection (seconds). Range [0.1, 1.0].")]
        public float DoubleClickWindow = 0.3f;

        [Tooltip("Pixel distance threshold to open radial menu on drag. Range [1, 20].")]
        public float RadialMenuDragThreshold = 5f;

        [Tooltip("Default approach distance for approach commands (meters).")]
        public float DefaultApproachDistance = 50f;

        [Tooltip("Default orbit distance for orbit commands (meters).")]
        public float DefaultOrbitDistance = 100f;

        [Tooltip("Default keep-at-range distance (meters).")]
        public float DefaultKeepAtRangeDistance = 50f;

        [Tooltip("Maximum range for mining beam activation (meters).")]
        public float MiningBeamMaxRange = 50f;

        private void OnValidate()
        {
            if (DoubleClickWindow < 0.1f || DoubleClickWindow > 1.0f)
                Debug.LogWarning($"[{name}] DoubleClickWindow must be in [0.1, 1.0]");
            if (RadialMenuDragThreshold < 1f || RadialMenuDragThreshold > 20f)
                Debug.LogWarning($"[{name}] RadialMenuDragThreshold must be in [1, 20]");
            if (DefaultApproachDistance <= 0f)
                Debug.LogWarning($"[{name}] DefaultApproachDistance must be > 0");
            if (DefaultOrbitDistance <= 0f)
                Debug.LogWarning($"[{name}] DefaultOrbitDistance must be > 0");
            if (DefaultKeepAtRangeDistance <= 0f)
                Debug.LogWarning($"[{name}] DefaultKeepAtRangeDistance must be > 0");
            if (MiningBeamMaxRange <= 0f)
                Debug.LogWarning($"[{name}] MiningBeamMaxRange must be > 0");
        }
    }
}
