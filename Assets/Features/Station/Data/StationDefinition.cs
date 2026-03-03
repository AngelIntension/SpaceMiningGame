using UnityEngine;
using VoidHarvest.Features.Base.Data;

namespace VoidHarvest.Features.Station.Data
{
    /// <summary>
    /// Single source of truth for one station's complete configuration.
    /// Designers create one asset per station and add to WorldDefinition.
    /// See Spec 009: Data-Driven World Config.
    /// </summary>
    [CreateAssetMenu(menuName = "VoidHarvest/Station/Station Definition")]
    public class StationDefinition : ScriptableObject
    {
        [Header("Identity")]
        [Tooltip("Unique station ID (> 0). Must be unique across WorldDefinition.")]
        public int StationId;

        [Tooltip("Human-readable station name shown in HUD.")]
        public string DisplayName;

        [Tooltip("Optional designer description.")]
        [TextArea(2, 4)]
        public string Description;

        [Tooltip("Classification of this station.")]
        public StationType StationType;

        [Header("World Placement")]
        [Tooltip("Station world position.")]
        public Vector3 WorldPosition;

        [Tooltip("Station world rotation.")]
        public Quaternion WorldRotation = Quaternion.identity;

        [Header("Services")]
        [Tooltip("List of service names available at this station (e.g., Sell, Refine, Repair, Cargo).")]
        public string[] AvailableServices;

        [Tooltip("Per-station service config (refining slots, speed multiplier, repair cost).")]
        public StationServicesConfig ServicesConfig;

        [Tooltip("Optional station preset config for procedural generation (Phase 2+).")]
        public StationPresetConfig PresetConfig;

        [Header("Docking")]
        [Tooltip("Docking port offset relative to station origin.")]
        public Vector3 DockingPortOffset;

        [Tooltip("Docking port rotation relative to station origin.")]
        public Quaternion DockingPortRotation = Quaternion.identity;

        [Tooltip("Safe direction to push ship on undock (normalized).")]
        public Vector3 SafeUndockDirection = Vector3.forward;

        [Header("Visuals")]
        [Tooltip("Optional station prefab for instantiation.")]
        public GameObject Prefab;

        [Tooltip("Optional station icon for HUD/UI.")]
        public Sprite Icon;

        private void OnValidate()
        {
            if (StationId <= 0)
                Debug.LogWarning($"[{name}] StationId must be > 0");
            if (string.IsNullOrEmpty(DisplayName))
                Debug.LogWarning($"[{name}] DisplayName must not be empty");
            if (ServicesConfig == null)
                Debug.LogWarning($"[{name}] ServicesConfig must not be null");
            if (AvailableServices == null || AvailableServices.Length < 1)
                Debug.LogWarning($"[{name}] AvailableServices must have at least one entry");
            if (DockingPortOffset.magnitude >= 200f)
                Debug.LogWarning($"[{name}] DockingPortOffset magnitude must be < 200");
        }
    }
}
