using UnityEngine;

namespace VoidHarvest.Features.Resources.Data
{
    /// <summary>
    /// Static data definition for a raw material type (processed output from refining).
    /// See Spec 006: Station Services.
    /// </summary>
    [CreateAssetMenu(menuName = "VoidHarvest/Station/Raw Material Definition")]
    public class RawMaterialDefinition : ScriptableObject
    {
        /// <summary>Unique identifier for this material type (e.g., "luminite_ingots").</summary>
        public string MaterialId;

        /// <summary>Human-readable name shown in UI (e.g., "Luminite Ingots").</summary>
        public string DisplayName;

        /// <summary>Inventory/UI icon.</summary>
        public Sprite Icon;

        /// <summary>Flavor text for tooltips.</summary>
        [TextArea]
        public string Description;

        /// <summary>Sell price per unit in integer credits.</summary>
        public int BaseValue;

        /// <summary>Cargo volume consumed per unit.</summary>
        public float VolumePerUnit;

        private void OnValidate()
        {
            if (string.IsNullOrEmpty(MaterialId))
                Debug.LogWarning($"[{name}] MaterialId must not be empty");
            if (string.IsNullOrEmpty(DisplayName))
                Debug.LogWarning($"[{name}] DisplayName must not be empty");
        }
    }
}
