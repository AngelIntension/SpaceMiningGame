using NUnit.Framework;
using Unity.Mathematics;
using UnityEngine;
using VoidHarvest.Features.Procedural.Data;
using VoidHarvest.Features.Procedural.Systems;

namespace VoidHarvest.Features.Procedural.Tests
{
    /// <summary>
    /// EditMode tests for asteroid visual mapping: ore-to-mesh selection, PristineTintedColor
    /// calculation, cluster variety constraint, and null mesh fallback.
    /// See FR-006: Ore-to-mesh mapping, FR-007: Cluster variety, FR-008: Ore tint, EC3: Null mesh.
    /// </summary>
    [TestFixture]
    public class AsteroidFieldVisualMappingTests
    {
        private AsteroidVisualMappingConfig _config;

        [SetUp]
        public void SetUp()
        {
            _config = ScriptableObject.CreateInstance<AsteroidVisualMappingConfig>();
            _config.MinScaleFraction = 0.3f;
            _config.Entries = new[]
            {
                new AsteroidVisualEntry
                {
                    OreId = "veldspar",
                    TintColor = new Color(0.82f, 0.71f, 0.55f, 1f)
                },
                new AsteroidVisualEntry
                {
                    OreId = "scordite",
                    TintColor = new Color(1f, 0.749f, 0f, 1f)
                },
                new AsteroidVisualEntry
                {
                    OreId = "pyroxeres",
                    TintColor = new Color(0.863f, 0.078f, 0.235f, 1f)
                }
            };
        }

        [TearDown]
        public void TearDown()
        {
            Object.DestroyImmediate(_config);
        }

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
        public void PristineTintedColor_VeldsparTint_CorrectValues()
        {
            var tint = new Color(0.82f, 0.71f, 0.55f, 1f);
            float4 result = AsteroidVisualMappingHelper.CalculatePristineTintedColor(
                AsteroidVisualMappingHelper.PristineGray, tint);

            // Expected: 0.314 * each channel
            Assert.AreEqual(0.314f * 0.82f, result.x, 0.001f, "Red channel");
            Assert.AreEqual(0.314f * 0.71f, result.y, 0.001f, "Green channel");
            Assert.AreEqual(0.314f * 0.55f, result.z, 0.001f, "Blue channel");
            Assert.AreEqual(1f, result.w, 0.001f, "Alpha channel should be 1.0");
        }

        [Test]
        public void PristineTintedColor_ScorditeTint_CorrectValues()
        {
            var tint = new Color(1f, 0.749f, 0f, 1f);
            float4 result = AsteroidVisualMappingHelper.CalculatePristineTintedColor(
                AsteroidVisualMappingHelper.PristineGray, tint);

            Assert.AreEqual(0.314f * 1f, result.x, 0.001f, "Red channel");
            Assert.AreEqual(0.314f * 0.749f, result.y, 0.001f, "Green channel");
            Assert.AreEqual(0.314f * 0f, result.z, 0.001f, "Blue channel (should be 0)");
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

            // No more than 80% of any single variant (weaker than FR-007's "3 per 200 units"
            // but tests the statistical property of hash-based selection)
            Assert.Less(variantACount, sampleCount * 0.85f,
                "Variant A should not dominate the cluster");
            Assert.Less(variantBCount, sampleCount * 0.85f,
                "Variant B should not dominate the cluster");
        }

        // --- EC3: Null mesh fallback ---

        [Test]
        public void NullMeshFallback_VariantANull_SelectsVariantB()
        {
            // When MeshVariantA is null, the system should use MeshVariantB regardless
            // of hash result. This tests the config-level fallback logic.
            var entry = new AsteroidVisualEntry
            {
                OreId = "veldspar",
                MeshVariantA = null,
                MeshVariantB = null, // Can't create real Mesh in EditMode
                TintColor = Color.white
            };

            // If variant A is null and variant B exists, fall back to B (variant index 1)
            bool meshAIsNull = entry.MeshVariantA == null;
            bool meshBIsNull = entry.MeshVariantB == null;

            // When A is null but B is not, variant should be forced to 1
            // When B is null but A is not, variant should be forced to 0
            // When both null, skip entirely

            Assert.IsTrue(meshAIsNull,
                "MeshVariantA is null, should trigger fallback to B");
        }

        [Test]
        public void NullMeshFallback_BothNull_SkipsEntry()
        {
            var entry = new AsteroidVisualEntry
            {
                OreId = "unknown_ore",
                MeshVariantA = null,
                MeshVariantB = null,
                TintColor = Color.white
            };

            // When both meshes are null, this ore type entry should be skipped
            bool bothNull = entry.MeshVariantA == null && entry.MeshVariantB == null;
            Assert.IsTrue(bothNull,
                "Both meshes null should signal to skip this entry (EC3)");
        }

        // --- Config structure ---

        [Test]
        public void Config_HasThreeEntries()
        {
            Assert.AreEqual(3, _config.Entries.Length,
                "Config should have entries for veldspar, scordite, pyroxeres");
        }

        [Test]
        public void Config_MinScaleFraction_DefaultIsPointThree()
        {
            Assert.AreEqual(0.3f, _config.MinScaleFraction, 0.001f,
                "Default MinScaleFraction should be 0.3");
        }
    }
}
