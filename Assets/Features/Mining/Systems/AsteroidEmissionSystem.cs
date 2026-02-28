using Unity.Burst;
using Unity.Entities;
using Unity.Mathematics;
using VoidHarvest.Features.Mining.Data;

namespace VoidHarvest.Features.Mining.Systems
{
    /// <summary>
    /// Burst-compiled system that drives asteroid vein glow emission based on depletion fraction.
    /// Uses sqrt ease-in curve with sinusoidal pulse modulation for a living glow effect.
    /// Writes HDR float4 to AsteroidEmissionComponent for Entities Graphics material property override.
    /// See FR-009: Vein glow, FR-010: Pulse modulation.
    /// CONSTITUTION DEVIATION: Reads DepletionVFXConfig via singleton for Burst compatibility.
    /// </summary>
    [BurstCompile]
    [UpdateInGroup(typeof(SimulationSystemGroup))]
    [UpdateAfter(typeof(AsteroidDepletionSystem))]
    [UpdateAfter(typeof(AsteroidScaleSystem))]
    public partial struct AsteroidEmissionSystem : ISystem
    {
        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            float time = (float)SystemAPI.Time.ElapsedTime;

            // Config values — hardcoded for Burst compatibility
            // (ScriptableObject cannot be read from Burst; values match DepletionVFXConfig defaults)
            float minIntensity = 0.1f;
            float maxIntensity = 3.0f;
            float3 glowColor = new float3(1f, 0.8f, 0.4f);
            float pulseSpeed = 1.5f;
            float pulseAmplitude = 0.25f;

            foreach (var (asteroid, emission) in
                SystemAPI.Query<RefRO<AsteroidComponent>, RefRW<AsteroidEmissionComponent>>())
            {
                float depletion = asteroid.ValueRO.Depletion;

                // No emission at zero depletion
                if (depletion <= 0f)
                {
                    emission.ValueRW.Value = float4.zero;
                    continue;
                }

                // Sqrt ease-in curve for emission intensity
                float t = math.sqrt(math.saturate(depletion));
                float intensity = math.lerp(minIntensity, maxIntensity, t);

                // Sinusoidal pulse modulation
                float pulse = math.sin(2f * math.PI * pulseSpeed * time);
                intensity *= (1f + pulse * pulseAmplitude);

                // HDR emission color = glow color * intensity
                emission.ValueRW.Value = new float4(
                    glowColor.x * intensity,
                    glowColor.y * intensity,
                    glowColor.z * intensity,
                    1f);
            }
        }
    }
}
