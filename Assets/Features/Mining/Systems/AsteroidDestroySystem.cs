using Unity.Burst;
using Unity.Entities;
using Unity.Mathematics;
using VoidHarvest.Features.Mining.Data;

namespace VoidHarvest.Features.Mining.Systems
{
    /// <summary>
    /// Burst-compiled system that handles asteroid fade-out and entity destruction.
    /// When FadeOutTimer is active: interpolates alpha from 1→0 via AsteroidBaseColorOverride,
    /// removes targeting components to prevent re-targeting during fade, and destroys the
    /// entity when the timer expires via EndSimulationEntityCommandBufferSystem.
    /// Runs after AsteroidScaleSystem in SimulationSystemGroup.
    /// See FR-021: Fade-out removal, SC-011: Removal timing.
    /// </summary>
    [BurstCompile]
    [UpdateInGroup(typeof(SimulationSystemGroup))]
    [UpdateAfter(typeof(AsteroidScaleSystem))]
    public partial struct AsteroidDestroySystem : ISystem
    {
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<EndSimulationEntityCommandBufferSystem.Singleton>();
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            float dt = SystemAPI.Time.DeltaTime;
            var ecb = SystemAPI.GetSingleton<EndSimulationEntityCommandBufferSystem.Singleton>()
                .CreateCommandBuffer(state.WorldUnmanaged);

            foreach (var (asteroid, baseColor, entity) in
                SystemAPI.Query<RefRW<AsteroidComponent>, RefRW<AsteroidBaseColorOverride>>()
                    .WithEntityAccess())
            {
                // Only process entities with active fade-out (timer > 0) or expired (timer < 0)
                if (asteroid.ValueRO.FadeOutTimer == 0f)
                    continue;

                if (asteroid.ValueRO.FadeOutTimer > 0f)
                {
                    // Decrement fade-out timer
                    asteroid.ValueRW.FadeOutTimer -= dt;

                    // Interpolate alpha from 1→0 for alpha clipping
                    float alpha = AsteroidDepletionFormulas.CalculateFadeAlpha(
                        asteroid.ValueRO.FadeOutTimer,
                        AsteroidDepletionFormulas.FadeOutDuration);

                    var color = baseColor.ValueRO.Value;
                    color.w = alpha;
                    baseColor.ValueRW.Value = color;
                }

                // Destroy entity when fade-out timer expires
                if (AsteroidDepletionFormulas.ShouldDestroy(asteroid.ValueRO.FadeOutTimer))
                {
                    ecb.DestroyEntity(entity);
                }
            }

            // Also process fade-out entities that still have ore component (remove targeting)
            // This runs separately to avoid query complexity with optional components
            foreach (var (asteroid, ore, entity) in
                SystemAPI.Query<RefRO<AsteroidComponent>, RefRO<AsteroidOreComponent>>()
                    .WithEntityAccess())
            {
                // Remove ore component when fade starts — makes entity untargetable by MiningBeamSystem
                if (asteroid.ValueRO.FadeOutTimer > 0f)
                {
                    ecb.RemoveComponent<AsteroidOreComponent>(entity);
                }
            }
        }
    }
}
