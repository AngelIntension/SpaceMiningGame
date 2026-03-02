using UnityEngine;
using UnityEngine.UIElements;
using VoidHarvest.Core.State;
using VoidHarvest.Features.Targeting.Data;
using VoidHarvest.Features.Targeting.Systems;

namespace VoidHarvest.Features.Targeting.Views
{
    /// <summary>
    /// Manages corner-bracket reticle UI elements for the selected target.
    /// Positioned per-frame via TargetingMath screen-space projection.
    /// See Spec 007: In-Flight Targeting (FR-001 through FR-004, FR-007).
    /// </summary>
    public sealed class ReticleView
    {
        private readonly TargetingConfig _config;
        private readonly VisualElement _container;
        private readonly VisualElement _cornerTL;
        private readonly VisualElement _cornerTR;
        private readonly VisualElement _cornerBL;
        private readonly VisualElement _cornerBR;
        private readonly Label _nameLabel;
        private readonly Label _typeLabel;
        private readonly Label _rangeLabel;
        private readonly Label _massLabel;

        private const float CornerSize = 12f;

        public bool IsVisible { get; private set; }

        public ReticleView(VisualElement root, TargetingConfig config)
        {
            _config = config;
            _container = root.Q("reticle-container");
            _cornerTL = root.Q("reticle-corner-tl");
            _cornerTR = root.Q("reticle-corner-tr");
            _cornerBL = root.Q("reticle-corner-bl");
            _cornerBR = root.Q("reticle-corner-br");
            _nameLabel = root.Q<Label>("target-name-label");
            _typeLabel = root.Q<Label>("target-type-label");
            _rangeLabel = root.Q<Label>("target-range-label");
            _massLabel = root.Q<Label>("target-mass-label");
        }

        public void Update(SelectionData selection, Vector3 targetWorldPos, float visualRadius,
                           Camera camera, Vector3 shipWorldPos)
        {
            if (!selection.HasSelection)
            {
                Hide();
                return;
            }

            Vector3 screenPos = camera.WorldToScreenPoint(targetWorldPos);
            float screenWidth = Screen.width;
            float screenHeight = Screen.height;

            if (!TargetingMath.IsInViewport(screenPos, screenWidth, screenHeight))
            {
                Hide();
                return;
            }

            Show();

            var bounds = TargetingMath.CalculateScreenBounds(
                new Unity.Mathematics.float3(targetWorldPos.x, targetWorldPos.y, targetWorldPos.z),
                visualRadius, camera);

            float size = Mathf.Max(bounds.width, bounds.height) + _config.ReticlePadding * 2f;
            size = Mathf.Clamp(size, _config.ReticleMinSize, _config.ReticleMaxSize);

            // Convert from screen-space (bottom-left origin) to UI Toolkit (top-left origin)
            float centerX = bounds.center.x;
            float centerY = screenHeight - bounds.center.y;

            _container.style.left = centerX - size * 0.5f;
            _container.style.top = centerY - size * 0.5f;
            _container.style.width = size;
            _container.style.height = size;

            SetPosition(_cornerTL, 0, 0);
            SetPosition(_cornerTR, size - CornerSize, 0);
            SetPosition(_cornerBL, 0, size - CornerSize);
            SetPosition(_cornerBR, size - CornerSize, size - CornerSize);

            if (_nameLabel != null) _nameLabel.text = selection.DisplayName;
            if (_typeLabel != null) _typeLabel.text = selection.TypeLabel;

            float range = Vector3.Distance(shipWorldPos, targetWorldPos);
            if (_rangeLabel != null) _rangeLabel.text = TargetingMath.FormatRange(range);
            if (_massLabel != null) _massLabel.text = "";
        }

        public void Show()
        {
            if (_container != null)
                _container.style.display = DisplayStyle.Flex;
            IsVisible = true;
        }

        public void Hide()
        {
            if (_container != null)
                _container.style.display = DisplayStyle.None;
            IsVisible = false;
        }

        private static void SetPosition(VisualElement element, float left, float top)
        {
            if (element == null) return;
            element.style.left = left;
            element.style.top = top;
        }
    }
}
