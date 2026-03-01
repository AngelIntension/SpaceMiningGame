using UnityEngine;

namespace VoidHarvest.Features.Mining.Data
{
    /// <summary>
    /// Static data definition for an ore type. Authored in Unity Editor.
    /// Contains all static data for spawning, mining, display, and future economy integration.
    /// See Spec 005: Data-Driven Ore System.
    /// </summary>
    [CreateAssetMenu(menuName = "VoidHarvest/Ore Definition")]
    public class OreDefinition : ScriptableObject
    {
        /// <summary>Unique string identifier for this ore type (e.g., "luminite").</summary>
        public string OreId;

        /// <summary>Human-readable name shown in HUD and inventory.</summary>
        public string DisplayName;

        /// <summary>Rarity classification (Common, Uncommon, Rare).</summary>
        public OreRarityTier RarityTier;

        /// <summary>Inventory/UI icon. Nullable — stored for future use.</summary>
        public Sprite Icon;

        /// <summary>Base market value per unit. Stored for future economy integration.</summary>
        public float BaseValue;

        /// <summary>Flavor text for tooltips. Stored for future use.</summary>
        [TextArea]
        public string Description;

        /// <summary>Default spawn probability weight [0, 1].</summary>
        [Range(0f, 1f)]
        public float RarityWeight;

        /// <summary>Base ore yield per second before hardness/depth modifiers.</summary>
        public float BaseYieldPerSecond;

        /// <summary>Extraction difficulty multiplier (denominator in yield formula).</summary>
        public float Hardness;

        /// <summary>Cargo volume consumed per unit mined.</summary>
        public float VolumePerUnit;

        /// <summary>Mining laser color when extracting this ore.</summary>
        public Color BeamColor;

        /// <summary>Refining time per unit in seconds. Stored for future use.</summary>
        public float BaseProcessingTimePerUnit;
    }
}
