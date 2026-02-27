using Unity.Burst;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using VoidHarvest.Features.Mining.Data;

namespace VoidHarvest.Features.Mining.Systems
{
    /// <summary>
    /// Burst-compiled system that applies mass-proportional scaling to asteroids,
    /// detects depletion threshold crossings for crumble pauses, and initiates
    /// fade-out on full depletion. Runs after AsteroidDepletionSystem.
    /// See FR-019: Depletion shrink, FR-020: Crumble pauses, FR-021: Fade-out initiation.
    /// </summary>
    [BurstCompile]
    [UpdateInGroup(typeof(SimulationSystemGroup))]
    [UpdateAfter(typeof(AsteroidDepletionSystem))]
    public partial struct AsteroidScaleSystem : ISystem
    {
        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            float dt = SystemAPI.Time.DeltaTime;

            // Read MinScaleFraction from baked config singleton (or default)
            float minScale = SystemAPI.HasSingleton<AsteroidVisualMappingSingleton>()
                ? SystemAPI.GetSingleton<AsteroidVisualMappingSingleton>().MinScaleFraction
                : 0.3f;

            foreach (var (asteroid, transform) in
                SystemAPI.Query<RefRW<AsteroidComponent>, RefRW<LocalTransform>>())
            {
                // Skip entities already in fade-out (handled by AsteroidDestroySystem)
                if (asteroid.ValueRO.FadeOutTimer > 0f)
                    continue;

                // --- Crumble pause logic ---
                if (asteroid.ValueRO.CrumblePauseTimer > 0f)
                {
                    // During crumble pause: freeze scale, decrement timer
                    asteroid.ValueRW.CrumblePauseTimer -= dt;
                    continue;
                }

                // --- Threshold detection ---
                float depletion = asteroid.ValueRO.Depletion;
                bool crossed = AsteroidDepletionFormulas.DetectThresholdCrossing(
                    depletion, asteroid.ValueRO.CrumbleThresholdsPassed, out byte newMask);

                if (crossed)
                {
                    asteroid.ValueRW.CrumbleThresholdsPassed = newMask;

                    // Check if this is the final threshold (100% depleted)
                    bool isFinalThreshold = (newMask & 0x08) != 0
                        && (asteroid.ValueRO.CrumbleThresholdsPassed & 0x08) == 0
                        || depletion >= 1.0f && (newMask & 0x08) != 0;

                    // Start crumble pause
                    asteroid.ValueRW.CrumblePauseTimer = AsteroidDepletionFormulas.CrumblePauseDuration;

                    // If final threshold, after this pause completes, AsteroidDestroySystem
                    // will start fade-out. We detect this by checking depletion >= 1.0 and
                    // CrumblePauseTimer reaching 0 in the next block.
                }

                // Check if final crumble pause just completed → start fade-out
                if (depletion >= 1.0f
                    && (asteroid.ValueRO.CrumbleThresholdsPassed & 0x08) != 0
                    && asteroid.ValueRO.CrumblePauseTimer <= 0f
                    && asteroid.ValueRO.FadeOutTimer == 0f)
                {
                    asteroid.ValueRW.FadeOutTimer = AsteroidDepletionFormulas.FadeOutDuration;
                    continue;
                }

                // --- Scale calculation ---
                float scale = AsteroidDepletionFormulas.CalculateScale(
                    asteroid.ValueRO.Radius,
                    asteroid.ValueRO.RemainingMass,
                    asteroid.ValueRO.InitialMass,
                    minScale);

                transform.ValueRW = LocalTransform.FromPositionRotationScale(
                    transform.ValueRO.Position,
                    transform.ValueRO.Rotation,
                    scale);
            }
        }
    }
}
