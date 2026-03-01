using Unity.Mathematics;
using UnityEngine;

namespace VoidHarvest.Features.Procedural.Systems
{
    /// <summary>
    /// Pure static helpers for asteroid visual mapping: mesh variant selection and tint calculation.
    /// Burst-compatible methods (no managed types). Called by AsteroidFieldSystem at spawn time.
    /// See FR-006: Ore-to-mesh mapping, FR-007: Cluster variety, FR-008: Ore tint.
    /// </summary>
    public static class AsteroidVisualMappingHelper
    {
        /// <summary>
        /// Select mesh variant (0 = A, 1 = B) based on world position hash.
        /// Deterministic: same position always selects the same variant.
        /// Provides spatial variety without neighbor queries (Burst-compatible).
        /// See FR-007: Cluster variety constraint.
        /// </summary>
        /// <param name="position">World-space position of the asteroid.</param>
        /// <returns>0 for variant A, 1 for variant B.</returns>
        public static int SelectMeshVariant(float3 position)
        {
            uint hash = math.hash(new int3(
                (int)math.floor(position.x),
                (int)math.floor(position.y),
                (int)math.floor(position.z)));
            return (int)((hash >> 16) & 1);
        }

        /// <summary>
        /// Calculate pristine tinted color by multiplying pristine gray with ore tint color.
        /// Result is stored on AsteroidComponent.PristineTintedColor at spawn time.
        /// See FR-008: Ore tint, data-model.md AsteroidComponent.PristineTintedColor.
        /// </summary>
        /// <param name="pristineGray">Base gray value (typically 0.314).</param>
        /// <param name="oreTint">Ore-specific tint color from AsteroidFieldDefinition.</param>
        /// <returns>Tinted pristine color as float4 (RGBA).</returns>
        public static float4 CalculatePristineTintedColor(float pristineGray, Color oreTint)
        {
            return new float4(
                pristineGray * oreTint.r,
                pristineGray * oreTint.g,
                pristineGray * oreTint.b,
                1f);
        }

        /// <summary>Pristine brightness multiplier for ore tint colors.
        /// At 1.0 the full tint color is applied, showing clear ore differentiation.</summary>
        public const float PristineGray = 1.0f;
    }
}
