using UnityEngine;
using UnityEngine.Rendering;
using VoidHarvest.Features.Camera.Data;

namespace VoidHarvest.Features.Camera.Views
{
    /// <summary>
    /// View-layer MonoBehaviour that applies skybox configuration to the scene.
    /// Reads SkyboxConfig reference, sets RenderSettings.skybox, configures ambient
    /// lighting, and rotates skybox each frame via _Rotation shader property.
    /// See FR-002: Nebula skybox, FR-003: Skybox rotation, FR-004: Ambient lighting, R-002.
    /// </summary>
    public class SkyboxController : MonoBehaviour
    {
        /// <summary>Per-scene skybox configuration. See FR-002.</summary>
        [Tooltip("Reference to the SkyboxConfig asset for this scene.")]
        [SerializeField]
        private SkyboxConfig _config;

        private Material _runtimeMaterial;
        private float _currentRotation;

        private static readonly int RotationProperty = Shader.PropertyToID("_Rotation");
        private static readonly int ExposureProperty = Shader.PropertyToID("_Exposure");

        private void Awake()
        {
            if (_config == null)
            {
                Debug.LogWarning("[VoidHarvest] SkyboxController: No SkyboxConfig assigned.");
                return;
            }

            ApplySkybox();
        }

        private void Update()
        {
            if (_runtimeMaterial == null) return;

            // Rotate skybox via _Rotation shader property (Skybox/Panoramic shader)
            _currentRotation += _config.RotationSpeed * Time.deltaTime;
            if (_currentRotation > 360f) _currentRotation -= 360f;
            _runtimeMaterial.SetFloat(RotationProperty, _currentRotation);
        }

        private void OnDestroy()
        {
            // Clean up runtime material instance to prevent memory leaks
            if (_runtimeMaterial != null)
            {
                Destroy(_runtimeMaterial);
                _runtimeMaterial = null;
            }
        }

        private void ApplySkybox()
        {
            var sourceMaterial = _config.GetEffectiveMaterial();
            if (sourceMaterial == null)
            {
                Debug.LogWarning("[VoidHarvest] SkyboxController: No skybox material available (both primary and fallback are null).");
                return;
            }

            // Create runtime instance to avoid modifying the shared asset
            _runtimeMaterial = new Material(sourceMaterial);
            RenderSettings.skybox = _runtimeMaterial;

            // Apply exposure override
            if (_runtimeMaterial.HasProperty(ExposureProperty))
            {
                _runtimeMaterial.SetFloat(ExposureProperty, _config.ExposureOverride);
            }

            // Configure ambient lighting to match HDRI
            RenderSettings.ambientMode = AmbientMode.Skybox;
            DynamicGI.UpdateEnvironment();

            Debug.Log($"[VoidHarvest] SkyboxController: Applied skybox '{sourceMaterial.name}' with rotation {_config.RotationSpeed} deg/s, exposure {_config.ExposureOverride}.");
        }
    }
}
