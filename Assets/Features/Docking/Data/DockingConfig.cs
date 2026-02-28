using UnityEngine;

namespace VoidHarvest.Features.Docking.Data
{
    /// <summary>
    /// Designer-tunable docking parameters. Single source of truth for ranges and timings.
    /// </summary>
    [CreateAssetMenu(fileName = "DockingConfig", menuName = "VoidHarvest/Docking/DockingConfig")]
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
    }
}
