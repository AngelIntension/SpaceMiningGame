using UnityEngine;
using VoidHarvest.Features.Resources.Data;

namespace VoidHarvest.Features.Mining.Data
{
    /// <summary>
    /// Serializable struct for ore refining output configuration.
    /// Each entry defines one raw material output and its yield variance.
    /// See Spec 006: Station Services.
    /// </summary>
    [System.Serializable]
    public struct RefiningOutputEntry
    {
        [Tooltip("Raw material produced by refining this ore.")]
        public RawMaterialDefinition Material;

        [Tooltip("Base output per input unit of ore.")]
        public int BaseYieldPerUnit;

        [Tooltip("Minimum additive offset per unit (can be negative).")]
        public int VarianceMin;

        [Tooltip("Maximum additive offset per unit (inclusive).")]
        public int VarianceMax;
    }
}
