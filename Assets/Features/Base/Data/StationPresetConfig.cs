using UnityEngine;

namespace VoidHarvest.Features.Base.Data
{
    /// <summary>
    /// Documents a station module assembly for prefab creation and future procedural generation.
    /// Currently informational — stations are pre-assembled prefabs. Will drive procedural
    /// generation in Phase 2+.
    /// See FR-013: Small Mining Relay, FR-014: Medium Refinery Hub.
    /// </summary>
    [CreateAssetMenu(menuName = "VoidHarvest/StationPresetConfig")]
    public class StationPresetConfig : ScriptableObject
    {
        /// <summary>Display name (e.g., "Small Mining Relay"). See FR-013, FR-014.</summary>
        [Tooltip("Human-readable station preset name.")]
        public string PresetName;

        /// <summary>Unique identifier for this station preset. See FR-013, FR-014.</summary>
        [Tooltip("Unique preset identifier.")]
        public string PresetId;

        /// <summary>Designer description of this station type. See FR-013, FR-014.</summary>
        [Tooltip("Description of this station preset.")]
        [TextArea(2, 4)]
        public string Description;

        /// <summary>Ordered list of module placements composing this station. See FR-013, FR-014.</summary>
        [Tooltip("Module composition for this station preset.")]
        public StationModuleEntry[] Modules;

    }

    /// <summary>
    /// A single module placement within a station preset.
    /// Defines what module, where it goes, and its functional role.
    /// See FR-013, FR-014: Station presets.
    /// </summary>
    [System.Serializable]
    public struct StationModuleEntry
    {
        /// <summary>Reference to a Station_MS2 prefab. See FR-013, FR-014.</summary>
        [Tooltip("Station module prefab (from Station_MS2 pack).")]
        public GameObject ModulePrefab;

        /// <summary>Position offset relative to station root. See FR-013, FR-014.</summary>
        [Tooltip("Local position relative to station root.")]
        public Vector3 LocalPosition;

        /// <summary>Rotation relative to station root. See FR-013, FR-014.</summary>
        [Tooltip("Local rotation relative to station root.")]
        public Quaternion LocalRotation;

        /// <summary>Functional role (e.g., "control", "storage", "energy"). See FR-013, FR-014.</summary>
        [Tooltip("Functional role of this module.")]
        public string ModuleRole;
    }
}
