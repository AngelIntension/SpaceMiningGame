using Unity.Burst;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Rendering;
using VoidHarvest.Features.Mining.Data;

namespace VoidHarvest.Features.Mining.Systems
{
    /// <summary>
    /// Updates AsteroidComponent.Depletion from RemainingMass/InitialMass,
    /// then writes URPMaterialPropertyBaseColor to drive per-entity color via Entities Graphics.
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
            var depleted = new float4(0.04f, 0.03f, 0.03f, 1f);

            foreach (var (asteroid, baseColor) in SystemAPI.Query<RefRW<AsteroidComponent>, RefRW<URPMaterialPropertyBaseColor>>())
            {
                if (asteroid.ValueRO.InitialMass > 0f)
                {
                    float depletion = 1f - (asteroid.ValueRO.RemainingMass / asteroid.ValueRO.InitialMass);
                    asteroid.ValueRW.Depletion = depletion;
                    // Ease-in curve makes early depletion more visible
                    float visual = math.sqrt(depletion);

                    // Use per-entity PristineTintedColor (ore-tinted, set at spawn) instead
                    // of hardcoded pristine constant. Falls back to default gray if not set.
                    // See FR-008: Ore tint, T019.
                    var pristine = asteroid.ValueRO.PristineTintedColor;
                    if (pristine.w < 0.01f)
                    {
                        // Fallback for entities spawned without tint (backward compatibility)
                        pristine = new float4(1f, 1f, 1f, 1f);
                    }

                    baseColor.ValueRW.Value = math.lerp(pristine, depleted, visual);
                }
            }
        }
    }
}
