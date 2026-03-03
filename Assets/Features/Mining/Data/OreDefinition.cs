using UnityEngine;

namespace VoidHarvest.Features.Mining.Data
{
    /// <summary>
    /// Static data definition for an ore type. Authored in Unity Editor.
    /// Contains all static data for spawning, mining, display, and future economy integration.
    /// See Spec 005: Data-Driven Ore System.
    /// </summary>
    [CreateAssetMenu(menuName = "VoidHarvest/Mining/Ore Definition")]
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

        /// <summary>Base market value per unit in integer credits.</summary>
        public int BaseValue;

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

        /// <summary>Refining time per unit in seconds.</summary>
        public float BaseProcessingTimePerUnit;

        /// <summary>Array of raw material outputs produced when refining this ore.</summary>
        [SerializeField]
        public RefiningOutputEntry[] RefiningOutputs = System.Array.Empty<RefiningOutputEntry>();

        /// <summary>Credit cost per unit of ore refined (integer).</summary>
        [SerializeField]
        public int RefiningCreditCostPerUnit;

        private void OnValidate()
        {
            if (string.IsNullOrEmpty(OreId))
                Debug.LogWarning($"[{name}] OreId must not be empty");
            if (string.IsNullOrEmpty(DisplayName))
                Debug.LogWarning($"[{name}] DisplayName must not be empty");
            if (BaseYieldPerSecond <= 0f)
                Debug.LogWarning($"[{name}] BaseYieldPerSecond must be > 0");
            if (Hardness <= 0f)
                Debug.LogWarning($"[{name}] Hardness must be > 0");
            if (VolumePerUnit <= 0f)
                Debug.LogWarning($"[{name}] VolumePerUnit must be > 0");
            if (BaseValue < 0)
                Debug.LogWarning($"[{name}] BaseValue must be >= 0");
            if (RarityWeight < 0f || RarityWeight > 1f)
                Debug.LogWarning($"[{name}] RarityWeight must be in [0, 1]");
            if (BaseProcessingTimePerUnit <= 0f)
                Debug.LogWarning($"[{name}] BaseProcessingTimePerUnit must be > 0");
            if (RefiningCreditCostPerUnit < 0)
                Debug.LogWarning($"[{name}] RefiningCreditCostPerUnit must be >= 0");
            if (RefiningOutputs != null)
            {
                for (int i = 0; i < RefiningOutputs.Length; i++)
                {
                    var entry = RefiningOutputs[i];
                    if (entry.Material == null)
                        Debug.LogWarning($"[{name}] RefiningOutputs[{i}].Material must not be null");
                    if (entry.BaseYieldPerUnit <= 0)
                        Debug.LogWarning($"[{name}] RefiningOutputs[{i}].BaseYieldPerUnit must be > 0");
                    if (entry.VarianceMin > entry.VarianceMax)
                        Debug.LogWarning($"[{name}] RefiningOutputs[{i}].VarianceMin must be <= VarianceMax");
                }
            }
        }
    }
}
