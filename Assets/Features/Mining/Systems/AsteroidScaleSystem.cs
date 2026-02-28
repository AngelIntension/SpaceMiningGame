using Unity.Burst;
using Unity.Collections;
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
    /// Enqueues NativeThresholdCrossedAction for MiningActionDispatchSystem to drain.
    /// See FR-019: Depletion shrink, FR-020: Crumble pauses, FR-021: Fade-out initiation.
    /// </summary>
    [BurstCompile]
    [UpdateInGroup(typeof(SimulationSystemGroup))]
    [UpdateAfter(typeof(AsteroidDepletionSystem))]
    public partial struct AsteroidScaleSystem : ISystem
    {
        private NativeQueue<NativeThresholdCrossedAction> _thresholdQueue;

        public NativeQueue<NativeThresholdCrossedAction> ThresholdQueue => _thresholdQueue;

        public void OnCreate(ref SystemState state)
        {
            _thresholdQueue = new NativeQueue<NativeThresholdCrossedAction>(Allocator.Persistent);
        }

        public void OnDestroy(ref SystemState state)
        {
            if (_thresholdQueue.IsCreated) _thresholdQueue.Dispose();
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            float dt = SystemAPI.Time.DeltaTime;

            // Read MinScaleFraction from baked config singleton (or default)
            float minScale = SystemAPI.HasSingleton<AsteroidVisualMappingSingleton>()
                ? SystemAPI.GetSingleton<AsteroidVisualMappingSingleton>().MinScaleFraction
                : 0.3f;

            foreach (var (asteroid, transform, entity) in
                SystemAPI.Query<RefRW<AsteroidComponent>, RefRW<LocalTransform>>().WithEntityAccess())
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
                byte oldMask = asteroid.ValueRO.CrumbleThresholdsPassed;
                bool crossed = AsteroidDepletionFormulas.DetectThresholdCrossing(
                    depletion, oldMask, out byte newMask);

                if (crossed)
                {
                    asteroid.ValueRW.CrumbleThresholdsPassed = newMask;

                    // Start crumble pause
                    asteroid.ValueRW.CrumblePauseTimer = AsteroidDepletionFormulas.CrumblePauseDuration;

                    // Enqueue threshold events for each newly crossed threshold
                    byte newBits = (byte)(newMask & ~oldMask);
                    for (byte i = 0; i < 4; i++)
                    {
                        if ((newBits & (1 << i)) != 0)
                        {
                            _thresholdQueue.Enqueue(new NativeThresholdCrossedAction
                            {
                                Asteroid = entity,
                                ThresholdIndex = i,
                                Position = transform.ValueRO.Position,
                                Radius = asteroid.ValueRO.Radius
                            });
                        }
                    }
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

                // Apply mesh normalization for non-unit-sized FBX meshes
                float normFactor = asteroid.ValueRO.MeshNormFactor;
                if (normFactor > 0f)
                    scale *= normFactor;

                transform.ValueRW = LocalTransform.FromPositionRotationScale(
                    transform.ValueRO.Position,
                    transform.ValueRO.Rotation,
                    scale);
            }
        }
    }
}
