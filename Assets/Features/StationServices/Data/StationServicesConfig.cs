using UnityEngine;

namespace VoidHarvest.Features.StationServices.Data
{
    /// <summary>
    /// Per-station service capabilities. Referenced from StationPresetConfig.
    /// See Spec 006: Station Services.
    /// </summary>
    [CreateAssetMenu(menuName = "VoidHarvest/Station Services Config")]
    public class StationServicesConfig : ScriptableObject
    {
        [Tooltip("Maximum number of concurrent active refining jobs at this station.")]
        public int MaxConcurrentRefiningSlots = 3;

        [Tooltip("Duration divisor for refining jobs. Higher = faster.")]
        public float RefiningSpeedMultiplier = 1.0f;

        [Tooltip("Credit cost per HP of hull damage repaired. 0 = no repair service.")]
        public int RepairCostPerHP = 100;
    }
}
