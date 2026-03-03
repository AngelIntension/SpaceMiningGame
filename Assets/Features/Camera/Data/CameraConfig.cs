using UnityEngine;

namespace VoidHarvest.Features.Camera.Data
{
    /// <summary>
    /// Designer-tunable camera parameters. Single source of truth for camera limits and defaults.
    /// See Spec 009: Data-Driven World Config (US3).
    /// </summary>
    [CreateAssetMenu(fileName = "CameraConfig", menuName = "VoidHarvest/Camera/Camera Config")]
    public class CameraConfig : ScriptableObject
    {
        [Header("Pitch Limits")]
        [Tooltip("Minimum pitch angle (degrees, should be negative for looking down).")]
        public float MinPitch = -80f;

        [Tooltip("Maximum pitch angle (degrees, should be positive for looking up).")]
        public float MaxPitch = 80f;

        [Header("Distance Limits")]
        [Tooltip("Minimum camera distance from target (meters).")]
        public float MinDistance = 5f;

        [Tooltip("Maximum camera distance from target (meters).")]
        public float MaxDistance = 50f;

        [Header("Speed Zoom Range")]
        [Tooltip("Closest zoom distance at full speed (meters). Must be >= MinDistance.")]
        public float MinZoomDistance = 10f;

        [Tooltip("Farthest zoom distance at zero speed (meters). Must be <= MaxDistance.")]
        public float MaxZoomDistance = 40f;

        [Header("Behavior")]
        [Tooltip("Duration after manual zoom before speed-zoom resumes (seconds).")]
        public float ZoomCooldownDuration = 2.0f;

        [Tooltip("Orbit sensitivity multiplier (degrees per pixel).")]
        public float OrbitSensitivity = 0.1f;

        [Header("Defaults")]
        [Tooltip("Default yaw angle on startup (degrees).")]
        public float DefaultYaw = 0f;

        [Tooltip("Default pitch angle on startup (degrees). Must be within [MinPitch, MaxPitch].")]
        public float DefaultPitch = 15f;

        [Tooltip("Default camera distance on startup (meters). Must be within [MinDistance, MaxDistance].")]
        public float DefaultDistance = 25f;

        private void OnValidate()
        {
            if (MinPitch >= 0f)
                Debug.LogWarning($"[{name}] MinPitch should be < 0");
            if (MaxPitch <= 0f)
                Debug.LogWarning($"[{name}] MaxPitch should be > 0");
            if (MinDistance <= 0f)
                Debug.LogWarning($"[{name}] MinDistance must be > 0");
            if (MaxDistance <= MinDistance)
                Debug.LogWarning($"[{name}] MaxDistance must be > MinDistance");
            if (MinZoomDistance < MinDistance)
                Debug.LogWarning($"[{name}] MinZoomDistance must be >= MinDistance");
            if (MaxZoomDistance > MaxDistance)
                Debug.LogWarning($"[{name}] MaxZoomDistance must be <= MaxDistance");
            if (ZoomCooldownDuration < 0f)
                Debug.LogWarning($"[{name}] ZoomCooldownDuration must be >= 0");
            if (OrbitSensitivity <= 0f)
                Debug.LogWarning($"[{name}] OrbitSensitivity must be > 0");
            if (DefaultPitch < MinPitch || DefaultPitch > MaxPitch)
                Debug.LogWarning($"[{name}] DefaultPitch must be within [MinPitch, MaxPitch]");
            if (DefaultDistance < MinDistance || DefaultDistance > MaxDistance)
                Debug.LogWarning($"[{name}] DefaultDistance must be within [MinDistance, MaxDistance]");
        }
    }
}
