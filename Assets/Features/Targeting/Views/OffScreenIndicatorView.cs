using UnityEngine;
using UnityEngine.UIElements;
using VoidHarvest.Features.Targeting.Data;
using VoidHarvest.Features.Targeting.Systems;

namespace VoidHarvest.Features.Targeting.Views
{
    /// <summary>
    /// Manages the off-screen directional indicator when the selected target
    /// is outside the camera viewport. Rotates toward the target direction.
    /// See Spec 007: In-Flight Targeting (FR-005, FR-005a).
    /// </summary>
    public sealed class OffScreenIndicatorView
    {
        private readonly TargetingConfig _config;
        private readonly VisualElement _indicator;

        public bool IsVisible { get; private set; }

        public OffScreenIndicatorView(VisualElement root, TargetingConfig config)
        {
            _config = config;
            _indicator = root.Q("offscreen-indicator");
        }

        public void Update(bool hasSelection, Vector3 screenPos, float screenWidth, float screenHeight)
        {
            if (!hasSelection || TargetingMath.IsInViewport(screenPos, screenWidth, screenHeight))
            {
                Hide();
                return;
            }

            Show();

            // Handle behind-camera case: mirror through screen center
            Vector2 pos2D;
            if (screenPos.z <= 0f)
            {
                pos2D = new Vector2(screenWidth - screenPos.x, screenHeight - screenPos.y);
            }
            else
            {
                pos2D = new Vector2(screenPos.x, screenPos.y);
            }

            var (clamped, angle) = TargetingMath.ClampToScreenEdge(
                pos2D,
                new Vector2(screenWidth, screenHeight),
                _config.OffScreenIndicatorMargin);

            // Convert from screen-space to UI Toolkit coordinates (Y flipped)
            float uiX = clamped.x - 8f;
            float uiY = screenHeight - clamped.y - 8f;

            _indicator.style.left = uiX;
            _indicator.style.top = uiY;
            _indicator.style.rotate = new Rotate(Angle.Degrees(-angle + 180f));
        }

        public void Show()
        {
            if (_indicator != null)
                _indicator.style.display = DisplayStyle.Flex;
            IsVisible = true;
        }

        public void Hide()
        {
            if (_indicator != null)
                _indicator.style.display = DisplayStyle.None;
            IsVisible = false;
        }
    }
}
