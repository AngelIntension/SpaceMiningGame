using UnityEngine;

namespace VoidHarvest.Features.Docking.Data
{
    /// <summary>
    /// Designer-tunable docking parameters. Single source of truth for ranges and timings.
    /// </summary>
    [CreateAssetMenu(fileName = "DockingConfig", menuName = "VoidHarvest/Docking/Docking Config")]
    public class DockingConfig : ScriptableObject
    {
        [Tooltip("Maximum range to initiate docking (meters).")]
        public float MaxDockingRange = 500f;

        [Tooltip("Range where magnetic snap animation begins (meters).")]
        public float SnapRange = 30f;

        [Tooltip("Duration of the snap animation (seconds).")]
        public float SnapDuration = 1.5f;

        [Tooltip("Distance the ship moves away from the port during undock (meters).")]
        public float UndockClearanceDistance = 100f;

        [Tooltip("Duration of the undock clearance movement (seconds).")]
        public float UndockDuration = 2f;

        [Tooltip("Safety timeout for approach phase before auto-cancel (seconds).")]
        public float ApproachTimeout = 120f;

        [Tooltip("Safety timeout for alignment phase before force-snap (seconds).")]
        public float AlignTimeout = 30f;

        [Tooltip("Dot product threshold for alignment completion (0–1, higher = stricter).")]
        public float AlignDotThreshold = 0.999f;

        [Tooltip("Angular velocity threshold for alignment settling (rad/s).")]
        public float AlignAngVelThreshold = 0.01f;

        private void OnValidate()
        {
            if (MaxDockingRange <= 0f)
                Debug.LogWarning($"[{name}] MaxDockingRange must be > 0");
            if (SnapRange <= 0f)
                Debug.LogWarning($"[{name}] SnapRange must be > 0");
            if (SnapRange >= MaxDockingRange)
                Debug.LogWarning($"[{name}] SnapRange must be < MaxDockingRange");
            if (SnapDuration <= 0f)
                Debug.LogWarning($"[{name}] SnapDuration must be > 0");
            if (UndockClearanceDistance <= 0f)
                Debug.LogWarning($"[{name}] UndockClearanceDistance must be > 0");
            if (UndockDuration <= 0f)
                Debug.LogWarning($"[{name}] UndockDuration must be > 0");
            if (ApproachTimeout <= 0f)
                Debug.LogWarning($"[{name}] ApproachTimeout must be > 0");
            if (AlignTimeout <= 0f)
                Debug.LogWarning($"[{name}] AlignTimeout must be > 0");
            if (AlignDotThreshold <= 0f || AlignDotThreshold > 1f)
                Debug.LogWarning($"[{name}] AlignDotThreshold must be in (0, 1]");
            if (AlignAngVelThreshold <= 0f)
                Debug.LogWarning($"[{name}] AlignAngVelThreshold must be > 0");
        }
    }
}
