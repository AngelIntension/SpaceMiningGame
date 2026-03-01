using UnityEngine;
using VoidHarvest.Features.Mining.Data;

namespace VoidHarvest.Features.Procedural.Data
{
    // CONSTITUTION DEVIATION: [Serializable] struct (not readonly) — Unity serialization
    // requires mutable fields for Inspector editing.
    /// <summary>
    /// A single ore entry within an AsteroidFieldDefinition.
    /// Links an OreDefinition to a spawn weight and visual mapping for that specific field.
    /// See Spec 005: Data-Driven Ore System.
    /// </summary>
    [System.Serializable]
    public struct OreFieldEntry
    {
        /// <summary>Reference to the ore type ScriptableObject.</summary>
        public OreDefinition OreDefinition;

        /// <summary>Relative spawn weight (normalized at runtime). Any positive value.</summary>
        public float Weight;

        /// <summary>First mesh variant for visual variety.</summary>
        public Mesh MeshVariantA;

        /// <summary>Second mesh variant for visual variety.</summary>
        public Mesh MeshVariantB;

        /// <summary>Ore-specific tint applied to asteroid material.</summary>
        public Color TintColor;
    }
}
