using UnityEngine;
using Unity.Mathematics;

namespace VoidHarvest.Features.Targeting.Systems
{
    /// <summary>
    /// Pure static class for targeting screen-space math.
    /// See Spec 007: In-Flight Targeting.
    /// </summary>
    public static class TargetingMath
    {
        /// <summary>
        /// Project a world-space sphere to a screen-space rectangle.
        /// </summary>
        public static Rect CalculateScreenBounds(float3 worldPosition, float visualRadius, Camera camera)
        {
            Vector3 screenCenter = camera.WorldToScreenPoint(new Vector3(worldPosition.x, worldPosition.y, worldPosition.z));
            if (screenCenter.z <= 0f)
                return new Rect(screenCenter.x, screenCenter.y, 0f, 0f);

            // Project the top of the sphere to get the apparent radius in screen space
            Vector3 worldTop = new Vector3(worldPosition.x, worldPosition.y + visualRadius, worldPosition.z);
            Vector3 screenTop = camera.WorldToScreenPoint(worldTop);
            float screenRadius = Mathf.Abs(screenTop.y - screenCenter.y);

            return new Rect(
                screenCenter.x - screenRadius,
                screenCenter.y - screenRadius,
                screenRadius * 2f,
                screenRadius * 2f);
        }

        /// <summary>
        /// Clamp a screen position to viewport edges with margin.
        /// Returns clamped position and angle (degrees) toward the original position.
        /// </summary>
        public static (Vector2 position, float angle) ClampToScreenEdge(
            Vector2 screenPos, Vector2 screenSize, float margin)
        {
            Vector2 center = screenSize * 0.5f;
            Vector2 direction = screenPos - center;

            Vector2 clamped = new Vector2(
                Mathf.Clamp(screenPos.x, margin, screenSize.x - margin),
                Mathf.Clamp(screenPos.y, margin, screenSize.y - margin));

            float angleDeg = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;

            return (clamped, angleDeg);
        }

        /// <summary>
        /// Check if a screen position is within the camera viewport.
        /// </summary>
        public static bool IsInViewport(Vector3 screenPos, float screenWidth, float screenHeight)
        {
            return screenPos.z > 0f &&
                   screenPos.x >= 0f && screenPos.x <= screenWidth &&
                   screenPos.y >= 0f && screenPos.y <= screenHeight;
        }

        /// <summary>
        /// Format distance in meters with thousands separator.
        /// </summary>
        public static string FormatRange(float distanceMeters)
        {
            int meters = Mathf.RoundToInt(distanceMeters);
            return $"{meters:N0} m";
        }
    }
}
