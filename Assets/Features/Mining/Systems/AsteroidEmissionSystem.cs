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
            float dt = SystemAPI.Time.DeltaTime;

            // Config values — hardcoded for Burst compatibility
            // (ScriptableObject cannot be read from Burst; values match DepletionVFXConfig defaults)
            // Intensity kept low because emission is applied uniformly (no vein mask texture).
            // Increase once per-vein emission maps are added to asteroid materials.
            float minIntensity = 0.0f;
            float maxIntensity = 0.6f;
            float3 glowColor = new float3(1f, 0.8f, 0.4f);
            float pulseSpeed = 1.5f;
            float pulseAmplitude = 0.15f;
            float fadeInSpeed = 3f;   // ~0.3s ramp-up
            float fadeOutSpeed = 0.5f; // ~2s decay

            // Find the asteroid currently being mined (if any)
            Entity activeTarget = Entity.Null;
            foreach (var beam in SystemAPI.Query<RefRO<MiningBeamComponent>>())
            {
                if (beam.ValueRO.Active)
                {
                    activeTarget = beam.ValueRO.TargetAsteroid;
                    break;
                }
            }

            foreach (var (asteroid, emission, glowFade, entity) in
                SystemAPI.Query<RefRO<AsteroidComponent>, RefRW<AsteroidEmissionComponent>, RefRW<AsteroidGlowFadeComponent>>()
                    .WithEntityAccess())
            {
                float depletion = asteroid.ValueRO.Depletion;
                bool isBeingMined = (entity == activeTarget) && depletion > 0f;

                // Update glow fade: ramp up while mined, decay when not
                float fade = glowFade.ValueRO.Value;
                fade = isBeingMined
                    ? math.min(fade + fadeInSpeed * dt, 1f)
                    : math.max(fade - fadeOutSpeed * dt, 0f);
                glowFade.ValueRW.Value = fade;

                if (fade <= 0f || depletion <= 0f)
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

                // Apply fade multiplier
                intensity *= fade;

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
