using NUnit.Framework;
using UnityEngine;
using VoidHarvest.Features.Camera.Data;

namespace VoidHarvest.Features.Camera.Tests
{
    /// <summary>
    /// EditMode tests for SkyboxConfig validation logic.
    /// See FR-002: Nebula skybox, FR-003: Rotation, FR-004: Ambient lighting, EC2: Fallback.
    /// </summary>
    [TestFixture]
    public class SkyboxConfigTests
    {
        private SkyboxConfig _config;

        [SetUp]
        public void SetUp()
        {
            _config = ScriptableObject.CreateInstance<SkyboxConfig>();
        }

        [TearDown]
        public void TearDown()
        {
            Object.DestroyImmediate(_config);
        }

        // --- GetEffectiveMaterial tests ---

        [Test]
        public void GetEffectiveMaterial_NullSkyboxMaterial_ReturnsFallback()
        {
            var fallback = new Material(Shader.Find("Universal Render Pipeline/Lit"));
            _config.SkyboxMaterial = null;
            _config.FallbackMaterial = fallback;

            var result = _config.GetEffectiveMaterial();

            Assert.AreSame(fallback, result,
                "Null SkyboxMaterial should return FallbackMaterial");

            Object.DestroyImmediate(fallback);
        }

        [Test]
        public void GetEffectiveMaterial_ValidSkyboxMaterial_ReturnsSkyboxMaterial()
        {
            var primary = new Material(Shader.Find("Universal Render Pipeline/Lit"));
            var fallback = new Material(Shader.Find("Universal Render Pipeline/Lit"));
            _config.SkyboxMaterial = primary;
            _config.FallbackMaterial = fallback;

            var result = _config.GetEffectiveMaterial();

            Assert.AreSame(primary, result,
                "Valid SkyboxMaterial should be returned over fallback");

            Object.DestroyImmediate(primary);
            Object.DestroyImmediate(fallback);
        }

        [Test]
        public void GetEffectiveMaterial_BothNull_ReturnsNull()
        {
            _config.SkyboxMaterial = null;
            _config.FallbackMaterial = null;

            var result = _config.GetEffectiveMaterial();

            Assert.IsNull(result,
                "Both null should return null");
        }

        // --- RotationSpeed validation tests ---

        [Test]
        public void Validate_RotationSpeedNegative_ClampedToZero()
        {
            _config.RotationSpeed = -5f;
            _config.Validate();

            Assert.AreEqual(0f, _config.RotationSpeed, 0.001f,
                "Negative rotation speed should be clamped to 0");
        }

        [Test]
        public void Validate_RotationSpeedExceedsMax_ClampedToFive()
        {
            _config.RotationSpeed = 100f;
            _config.Validate();

            Assert.AreEqual(5f, _config.RotationSpeed, 0.001f,
                "Rotation speed exceeding max should be clamped to 5");
        }

        [Test]
        public void Validate_RotationSpeedInRange_Unchanged()
        {
            _config.RotationSpeed = 2.5f;
            _config.Validate();

            Assert.AreEqual(2.5f, _config.RotationSpeed, 0.001f,
                "In-range rotation speed should remain unchanged");
        }

        // --- ExposureOverride validation tests ---

        [Test]
        public void Validate_ExposureBelowMin_ClampedToPointOne()
        {
            _config.ExposureOverride = 0f;
            _config.Validate();

            Assert.AreEqual(0.1f, _config.ExposureOverride, 0.001f,
                "Exposure below min should be clamped to 0.1");
        }

        [Test]
        public void Validate_ExposureAboveMax_ClampedToThree()
        {
            _config.ExposureOverride = 10f;
            _config.Validate();

            Assert.AreEqual(3f, _config.ExposureOverride, 0.001f,
                "Exposure above max should be clamped to 3.0");
        }

        [Test]
        public void Validate_ExposureInRange_Unchanged()
        {
            _config.ExposureOverride = 1.5f;
            _config.Validate();

            Assert.AreEqual(1.5f, _config.ExposureOverride, 0.001f,
                "In-range exposure should remain unchanged");
        }

        // --- Default values ---

        [Test]
        public void DefaultRotationSpeed_IsHalf()
        {
            Assert.AreEqual(0.5f, _config.RotationSpeed, 0.001f,
                "Default rotation speed should be 0.5");
        }

        [Test]
        public void DefaultExposure_IsOne()
        {
            Assert.AreEqual(1.0f, _config.ExposureOverride, 0.001f,
                "Default exposure should be 1.0");
        }
    }
}
