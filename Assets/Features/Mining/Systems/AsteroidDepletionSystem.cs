using Unity.Burst;
using Unity.Entities;
using Unity.Mathematics;
using VoidHarvest.Features.Mining.Data;

namespace VoidHarvest.Features.Mining.Systems
{
    /// <summary>
    /// Updates AsteroidComponent.Depletion from RemainingMass/InitialMass,
    /// then writes AsteroidBaseColorOverride to drive per-entity color via Entities Graphics.
    /// See MVP-07: Asteroid depletion visual.
    /// </summary>
    [BurstCompile]
    [UpdateInGroup(typeof(SimulationSystemGroup))]
    [UpdateAfter(typeof(MiningBeamSystem))]
    public partial struct AsteroidDepletionSystem : ISystem
    {
        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            var pristine = new float4(0.314f, 0.314f, 0.314f, 1f);
            var depleted = new float4(0.04f, 0.03f, 0.03f, 1f);

            foreach (var (asteroid, baseColor) in SystemAPI.Query<RefRW<AsteroidComponent>, RefRW<AsteroidBaseColorOverride>>())
            {
                if (asteroid.ValueRO.InitialMass > 0f)
                {
                    float depletion = 1f - (asteroid.ValueRO.RemainingMass / asteroid.ValueRO.InitialMass);
                    asteroid.ValueRW.Depletion = depletion;
                    // Ease-in curve makes early depletion more visible
                    float visual = math.sqrt(depletion);
                    baseColor.ValueRW.Value = math.lerp(pristine, depleted, visual);
                }
            }
        }
    }
}
