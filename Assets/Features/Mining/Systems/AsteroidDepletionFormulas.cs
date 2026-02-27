using Unity.Mathematics;

namespace VoidHarvest.Features.Mining.Systems
{
    /// <summary>
    /// Pure static formulas for asteroid depletion visuals: scale, thresholds, fade-out.
    /// Burst-compatible (no managed types). Called by AsteroidScaleSystem and AsteroidDestroySystem.
    /// See FR-018 through FR-021: Mass-proportional size, depletion shrink, crumble pauses, fade-out removal.
    /// </summary>
    public static class AsteroidDepletionFormulas
    {
        /// <summary>
        /// Calculate asteroid visual scale based on remaining mass fraction.
        /// Formula: radius * lerp(minScaleFraction, 1.0, remainingMass / initialMass).
        /// See FR-019: Depletion shrink, SC-010: Visible shrinkage.
        /// </summary>
        /// <param name="radius">Asteroid radius in meters.</param>
        /// <param name="remainingMass">Current mass after mining extraction.</param>
        /// <param name="initialMass">Original mass at spawn time.</param>
        /// <param name="minScaleFraction">Minimum scale multiplier at full depletion (prevents invisible asteroids).</param>
        /// <returns>Visual scale for LocalTransform.</returns>
        public static float CalculateScale(float radius, float remainingMass, float initialMass, float minScaleFraction)
        {
            float fraction = initialMass > 0f ? remainingMass / initialMass : 0f;
            fraction = math.saturate(fraction);
            return radius * math.lerp(minScaleFraction, 1f, fraction);
        }

        /// <summary>
        /// Detect depletion threshold crossings via bitmask.
        /// Thresholds: bit0 = 25% depleted (75% remaining), bit1 = 50%, bit2 = 75%, bit3 = 100%.
        /// Returns updated bitmask and whether any new threshold was crossed.
        /// See FR-020: Crumble pauses at 75%, 50%, 25%, 0% remaining.
        /// </summary>
        /// <param name="depletionFraction">Current depletion [0, 1] where 1 = fully depleted.</param>
        /// <param name="currentMask">Current threshold bitmask.</param>
        /// <param name="newMask">Updated bitmask with newly crossed thresholds.</param>
        /// <returns>True if any new threshold was crossed.</returns>
        public static bool DetectThresholdCrossing(float depletionFraction, byte currentMask, out byte newMask)
        {
            newMask = currentMask;
            bool crossed = false;

            // bit0: 25% depleted (75% remaining)
            if (depletionFraction >= 0.25f && (currentMask & 0x01) == 0)
            {
                newMask |= 0x01;
                crossed = true;
            }

            // bit1: 50% depleted (50% remaining)
            if (depletionFraction >= 0.50f && (currentMask & 0x02) == 0)
            {
                newMask |= 0x02;
                crossed = true;
            }

            // bit2: 75% depleted (25% remaining)
            if (depletionFraction >= 0.75f && (currentMask & 0x04) == 0)
            {
                newMask |= 0x04;
                crossed = true;
            }

            // bit3: 100% depleted (0% remaining)
            if (depletionFraction >= 1.00f && (currentMask & 0x08) == 0)
            {
                newMask |= 0x08;
                crossed = true;
            }

            return crossed;
        }

        /// <summary>
        /// Calculate fade-out alpha from timer and duration.
        /// Alpha interpolates 1 → 0 as timer counts down from duration to 0.
        /// See FR-021: Fade-out removal.
        /// </summary>
        /// <param name="fadeOutTimer">Current fade-out timer value (counting down).</param>
        /// <param name="fadeOutDuration">Total fade-out duration in seconds.</param>
        /// <returns>Alpha value [0, 1].</returns>
        public static float CalculateFadeAlpha(float fadeOutTimer, float fadeOutDuration)
        {
            if (fadeOutDuration <= 0f) return 0f;
            return math.saturate(fadeOutTimer / fadeOutDuration);
        }

        /// <summary>
        /// Check if an asteroid entity should be destroyed (fade-out complete).
        /// See FR-021: Fade-out removal.
        /// </summary>
        /// <param name="fadeOutTimer">Current fade-out timer value.</param>
        /// <returns>True if timer has expired (entity should be destroyed).</returns>
        public static bool ShouldDestroy(float fadeOutTimer)
        {
            return fadeOutTimer < 0f;
        }

        /// <summary>Duration of crumble pause at each threshold in seconds.</summary>
        public const float CrumblePauseDuration = 0.5f;

        /// <summary>Duration of fade-out animation in seconds.</summary>
        public const float FadeOutDuration = 0.5f;
    }
}
