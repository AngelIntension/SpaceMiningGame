using UnityEngine;

namespace VoidHarvest.Features.Procedural.Data
{
    /// <summary>
    /// Maps ore types to premium asteroid mesh variants and tint colors.
    /// Designer-configured: each entry maps an OreId to two mesh variants and a tint.
    /// See FR-006: Ore-to-mesh mapping, FR-007: Cluster variety, FR-008: Ore tint.
    /// </summary>
    [CreateAssetMenu(menuName = "VoidHarvest/AsteroidVisualMappingConfig")]
    public class AsteroidVisualMappingConfig : ScriptableObject
    {
        /// <summary>
        /// Ore-type-to-visual mappings. Order should match OreDistributions in AsteroidFieldConfig
        /// for efficient index-based lookup. See FR-006.
        /// </summary>
        [Tooltip("Each entry maps an ore type to two mesh variants and a tint color.")]
        public AsteroidVisualEntry[] Entries;

        /// <summary>
        /// Minimum scale multiplier at full depletion (prevents asteroids from becoming
        /// too small to see/target before removal). See FR-019: Depletion shrink.
        /// </summary>
        [Tooltip("Minimum scale at full depletion (default 0.3).")]
        [Range(0.1f, 0.5f)]
        public float MinScaleFraction = 0.3f;
    }

    /// <summary>
    /// Single ore-type-to-visual mapping entry. Maps an OreId to two mesh variants and a tint color.
    /// See FR-006: Ore-to-mesh mapping.
    /// </summary>
    [System.Serializable]
    public struct AsteroidVisualEntry
    {
        /// <summary>
        /// Matches OreTypeDefinition.OreId (e.g., "veldspar"). Named OreId (not OreTypeId)
        /// to avoid confusion with AsteroidOreComponent.OreTypeId (int index at runtime).
        /// See FR-006.
        /// </summary>
        [Tooltip("Ore type identifier matching OreTypeDefinition.OreId.")]
        public string OreId;

        /// <summary>First mesh variant for this ore type. See FR-006.</summary>
        [Tooltip("First mesh variant prefab.")]
        public Mesh MeshVariantA;

        /// <summary>Second mesh variant for this ore type. See FR-006.</summary>
        [Tooltip("Second mesh variant prefab.")]
        public Mesh MeshVariantB;

        /// <summary>
        /// Subtle color tint matching the ore's beam color. Applied as multiplicative
        /// layer over pristine gray during spawning. See FR-008: Ore tint.
        /// </summary>
        [Tooltip("Tint color for this ore type (matches beam color).")]
        public Color TintColor;
    }
}
