using UnityEngine;

namespace VoidHarvest.Features.Mining.Data
{
    /// <summary>
    /// Static data definition for an ore type. Authored in Unity Editor.
    /// See MVP-05: Mining beam and yield calculation.
    /// </summary>
    [CreateAssetMenu(menuName = "VoidHarvest/OreTypeDefinition")]
    public class OreTypeDefinition : ScriptableObject
    {
        /// <summary>Unique string identifier for this ore type. See MVP-05.</summary>
        public string OreId;
        /// <summary>Human-readable name shown in HUD and inventory. See MVP-09.</summary>
        public string DisplayName;
        /// <summary>Color of the mining beam when extracting this ore. See MVP-05.</summary>
        public Color BeamColor;
        /// <summary>Base ore yield per second before hardness/depth modifiers. See MVP-05.</summary>
        public float BaseYieldPerSecond;
        /// <summary>Extraction difficulty multiplier (denominator in yield formula). See MVP-05.</summary>
        public float Hardness;
        /// <summary>Ore tier for progression gating. See MVP-05.</summary>
        public int Tier;
        /// <summary>Spawn rarity weight [0, 1] for procedural field generation. See MVP-07.</summary>
        [Range(0f, 1f)]
        public float Rarity;
        /// <summary>Cargo volume consumed per unit mined. See MVP-06.</summary>
        public float VolumePerUnit;
    }
}
