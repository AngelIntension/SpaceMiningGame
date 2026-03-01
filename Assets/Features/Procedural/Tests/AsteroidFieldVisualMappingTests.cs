using NUnit.Framework;
using Unity.Mathematics;
using UnityEngine;
using VoidHarvest.Features.Procedural.Systems;

namespace VoidHarvest.Features.Procedural.Tests
{
    /// <summary>
    /// EditMode tests for asteroid visual mapping: mesh variant selection, PristineTintedColor
    /// calculation, and cluster variety constraint.
    /// See FR-006: Ore-to-mesh mapping, FR-007: Cluster variety, FR-008: Ore tint.
    /// </summary>
    [TestFixture]
    public class AsteroidFieldVisualMappingTests
    {
        // --- Ore-to-mesh selection ---

        [Test]
        public void SelectMeshVariant_ReturnsZeroOrOne()
        {
            var position = new float3(100f, 200f, 300f);
            int variant = AsteroidVisualMappingHelper.SelectMeshVariant(position);

            Assert.IsTrue(variant == 0 || variant == 1,
                "Variant selection should return 0 (A) or 1 (B)");
        }

        [Test]
        public void SelectMeshVariant_SamePosition_ReturnsSameVariant()
        {
            var position = new float3(42f, 17f, -100f);
            int variant1 = AsteroidVisualMappingHelper.SelectMeshVariant(position);
            int variant2 = AsteroidVisualMappingHelper.SelectMeshVariant(position);

            Assert.AreEqual(variant1, variant2,
                "Same position should always select the same variant (deterministic)");
        }

        [Test]
        public void SelectMeshVariant_DifferentPositions_ProducesBothVariants()
        {
            // Test that over many positions, both variants appear
            bool hasZero = false;
            bool hasOne = false;

            for (int i = 0; i < 100; i++)
            {
                var pos = new float3(i * 10f, i * 7f, i * 13f);
                int variant = AsteroidVisualMappingHelper.SelectMeshVariant(pos);
                if (variant == 0) hasZero = true;
                if (variant == 1) hasOne = true;
            }

            Assert.IsTrue(hasZero && hasOne,
                "Both variants should appear across diverse positions");
        }

        // --- PristineTintedColor calculation (FR-008) ---

        [Test]
        public void PristineTintedColor_LuminiteTint_CorrectValues()
        {
            var tint = new Color(0.6f, 0.85f, 1f, 1f);
            float4 result = AsteroidVisualMappingHelper.CalculatePristineTintedColor(
                AsteroidVisualMappingHelper.PristineGray, tint);

            Assert.AreEqual(AsteroidVisualMappingHelper.PristineGray * 0.6f, result.x, 0.001f, "Red channel");
            Assert.AreEqual(AsteroidVisualMappingHelper.PristineGray * 0.85f, result.y, 0.001f, "Green channel");
            Assert.AreEqual(AsteroidVisualMappingHelper.PristineGray * 1f, result.z, 0.001f, "Blue channel");
            Assert.AreEqual(1f, result.w, 0.001f, "Alpha channel should be 1.0");
        }

        [Test]
        public void PristineTintedColor_FerroxTint_CorrectValues()
        {
            var tint = new Color(0.8f, 0.5f, 0.2f, 1f);
            float4 result = AsteroidVisualMappingHelper.CalculatePristineTintedColor(
                AsteroidVisualMappingHelper.PristineGray, tint);

            Assert.AreEqual(AsteroidVisualMappingHelper.PristineGray * 0.8f, result.x, 0.001f, "Red channel");
            Assert.AreEqual(AsteroidVisualMappingHelper.PristineGray * 0.5f, result.y, 0.001f, "Green channel");
            Assert.AreEqual(AsteroidVisualMappingHelper.PristineGray * 0.2f, result.z, 0.001f, "Blue channel");
            Assert.AreEqual(1f, result.w, 0.001f, "Alpha channel should be 1.0");
        }

        [Test]
        public void PristineTintedColor_WhiteTint_ReturnsPristineGray()
        {
            var white = new Color(1f, 1f, 1f, 1f);
            float4 result = AsteroidVisualMappingHelper.CalculatePristineTintedColor(
                AsteroidVisualMappingHelper.PristineGray, white);

            float g = AsteroidVisualMappingHelper.PristineGray;
            Assert.AreEqual(g, result.x, 0.001f, "White tint should preserve pristine gray");
            Assert.AreEqual(g, result.y, 0.001f);
            Assert.AreEqual(g, result.z, 0.001f);
        }

        // --- FR-007: Cluster variety constraint ---

        [Test]
        public void ClusterVariety_NearbyPositions_NotAllSameVariant()
        {
            // Generate positions within a 200-unit radius cluster
            int variantACount = 0;
            int variantBCount = 0;
            int sampleCount = 20;

            for (int i = 0; i < sampleCount; i++)
            {
                // Positions within a 200-unit cube centered at origin
                var pos = new float3(
                    (i % 5) * 40f,
                    ((i / 5) % 4) * 50f,
                    (i * 17) % 200);
                int variant = AsteroidVisualMappingHelper.SelectMeshVariant(pos);
                if (variant == 0) variantACount++;
                else variantBCount++;
            }

            // Neither variant should dominate completely — both should be represented
            Assert.Greater(variantACount, 0,
                "Variant A should appear at least once in cluster");
            Assert.Greater(variantBCount, 0,
                "Variant B should appear at least once in cluster");

            // No more than 80% of any single variant
            Assert.Less(variantACount, sampleCount * 0.85f,
                "Variant A should not dominate the cluster");
            Assert.Less(variantBCount, sampleCount * 0.85f,
                "Variant B should not dominate the cluster");
        }
    }
}
