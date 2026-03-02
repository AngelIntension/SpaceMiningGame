using UnityEngine;
using UnityEngine.UIElements;
using VoidHarvest.Core.State;
using VoidHarvest.Features.Targeting.Data;

namespace VoidHarvest.Features.Targeting.Views
{
    /// <summary>
    /// Manages lock acquisition progress overlay on top of the reticle.
    /// Renders a progress ring, pulses corners during acquisition, and flashes on completion.
    /// The reticle and its labels remain visible underneath (FR-033).
    /// See Spec 007: In-Flight Targeting (FR-010, FR-012, FR-014, FR-033).
    /// </summary>
    public sealed class LockProgressView
    {
        private readonly TargetingConfig _config;
        private readonly TargetingVFXConfig _vfxConfig;
        private readonly VisualElement _container;
        private readonly VisualElement _ring;
        private readonly VisualElement _flash;

        private float _flashTimer;
        private bool _wasActive;

        public LockProgressView(VisualElement root, TargetingConfig config, TargetingVFXConfig vfxConfig)
        {
            _config = config;
            _vfxConfig = vfxConfig;
            _container = root.Q("lock-progress-container");

            if (_container != null)
            {
                _ring = new VisualElement();
                _ring.AddToClassList("lock-progress-ring");
                _container.Add(_ring);

                _flash = new VisualElement();
                _flash.AddToClassList("lock-flash");
                _container.Add(_flash);
            }
        }

        public void Update(LockAcquisitionData acquisition, float reticleLeft, float reticleTop,
                           float reticleSize, float[] cornerOpacities)
        {
            if (_container == null) return;

            // Update flash timer
            if (_flashTimer > 0f)
            {
                _flashTimer -= Time.deltaTime;
                float flashAlpha = Mathf.Clamp01(_flashTimer / _vfxConfig.LockFlashDuration);
                if (_flash != null)
                    _flash.style.backgroundColor = new Color(1f, 1f, 1f, flashAlpha * 0.6f);
            }
            else if (_flash != null)
            {
                _flash.style.backgroundColor = new Color(1f, 1f, 1f, 0f);
            }

            if (!acquisition.IsActive)
            {
                // Detect completion transition
                if (_wasActive && acquisition.Status == LockAcquisitionStatus.Completed)
                {
                    TriggerFlash();
                }
                _wasActive = false;

                if (_flashTimer <= 0f)
                    Hide();
                return;
            }

            _wasActive = true;
            Show();

            // Position to match reticle
            _container.style.left = reticleLeft;
            _container.style.top = reticleTop;
            _container.style.width = reticleSize;
            _container.style.height = reticleSize;

            // Progress ring: border opacity increases with progress
            float progress = acquisition.Progress;
            float ringAlpha = progress * 0.9f;
            var ringColor = new Color(0f, 0.78f, 1f, ringAlpha);
            if (_ring != null)
            {
                _ring.style.borderTopColor = ringColor;
                _ring.style.borderRightColor = ringColor;
                _ring.style.borderBottomColor = ringColor;
                _ring.style.borderLeftColor = ringColor;
                float ringWidth = _config.LockProgressArcWidth;
                _ring.style.borderTopWidth = ringWidth;
                _ring.style.borderRightWidth = ringWidth;
                _ring.style.borderBottomWidth = ringWidth;
                _ring.style.borderLeftWidth = ringWidth;
            }

            // Corner pulse via sin wave
            if (cornerOpacities != null && cornerOpacities.Length == 4)
            {
                float pulse = 0.6f + 0.4f * Mathf.Sin(Time.time * _vfxConfig.ReticlePulseSpeed * Mathf.PI * 2f);
                cornerOpacities[0] = pulse;
                cornerOpacities[1] = pulse;
                cornerOpacities[2] = pulse;
                cornerOpacities[3] = pulse;
            }
        }

        public void TriggerFlash()
        {
            _flashTimer = _vfxConfig != null ? _vfxConfig.LockFlashDuration : 0.3f;
        }

        public void Show()
        {
            if (_container != null)
                _container.style.display = DisplayStyle.Flex;
        }

        public void Hide()
        {
            if (_container != null)
                _container.style.display = DisplayStyle.None;
        }
    }
}
