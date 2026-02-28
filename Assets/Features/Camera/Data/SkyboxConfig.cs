using UnityEngine;

namespace VoidHarvest.Features.Camera.Data
{
    /// <summary>
    /// Per-scene skybox configuration. References one of the Nebula HDRI materials
    /// and controls rotation speed and exposure. Read-only at runtime.
    /// See FR-002: Nebula skybox, FR-003: Skybox rotation, FR-004: Ambient lighting.
    /// </summary>
    [CreateAssetMenu(menuName = "VoidHarvest/SkyboxConfig")]
    public class SkyboxConfig : ScriptableObject
    {
        /// <summary>Primary skybox material (Skybox/Panoramic shader with HDRI). See FR-002.</summary>
        [Tooltip("Reference to a Nebula HDRI material (Skybox/Panoramic shader).")]
        public Material SkyboxMaterial;

        /// <summary>Fallback material if primary is null. See FR-002, EC2.</summary>
        [Tooltip("Fallback skybox material (e.g., SpaceSkybox.mat).")]
        public Material FallbackMaterial;

        /// <summary>Skybox rotation speed in degrees per second. See FR-003.</summary>
        [Tooltip("Rotation speed in degrees/second.")]
        [Range(0f, 5f)]
        public float RotationSpeed = 0.5f;

        /// <summary>Exposure adjustment for skybox brightness. See FR-004.</summary>
        [Tooltip("Exposure override (1.0 = default).")]
        [Range(0.1f, 3f)]
        public float ExposureOverride = 1.0f;

        /// <summary>
        /// Returns SkyboxMaterial if assigned, otherwise FallbackMaterial.
        /// See EC2: Material load failure.
        /// </summary>
        public Material GetEffectiveMaterial()
        {
            return SkyboxMaterial != null ? SkyboxMaterial : FallbackMaterial;
        }

        /// <summary>
        /// Clamp fields to valid ranges. Called by OnValidate and available for tests.
        /// See FR-003: Rotation speed range, FR-004: Exposure range.
        /// </summary>
        public void Validate()
        {
            RotationSpeed = Mathf.Clamp(RotationSpeed, 0f, 5f);
            ExposureOverride = Mathf.Clamp(ExposureOverride, 0.1f, 3f);
        }

        private void OnValidate()
        {
            Validate();
        }
    }
}
