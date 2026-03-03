using UnityEngine;

namespace VoidHarvest.Features.StationServices.Data
{
    /// <summary>
    /// Per-station service capabilities. Referenced from StationPresetConfig.
    /// See Spec 006: Station Services.
    /// </summary>
    [CreateAssetMenu(menuName = "VoidHarvest/Station/Station Services Config")]
    public class StationServicesConfig : ScriptableObject
    {
        [Tooltip("Maximum number of concurrent active refining jobs at this station.")]
        public int MaxConcurrentRefiningSlots = 3;

        [Tooltip("Duration divisor for refining jobs. Higher = faster.")]
        public float RefiningSpeedMultiplier = 1.0f;

        [Tooltip("Credit cost per HP of hull damage repaired. 0 = no repair service.")]
        public int RepairCostPerHP = 100;

        private void OnValidate()
        {
            if (MaxConcurrentRefiningSlots < 1)
                Debug.LogWarning($"[{name}] MaxConcurrentRefiningSlots must be >= 1");
            if (RefiningSpeedMultiplier <= 0f)
                Debug.LogWarning($"[{name}] RefiningSpeedMultiplier must be > 0");
            if (RepairCostPerHP < 0)
                Debug.LogWarning($"[{name}] RepairCostPerHP must be >= 0");
        }
    }
}
