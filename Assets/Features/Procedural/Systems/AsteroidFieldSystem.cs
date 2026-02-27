using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;
using VoidHarvest.Features.Mining.Data;
using VoidHarvest.Features.Procedural.Data;
using VoidHarvest.Features.Procedural.Views;

namespace VoidHarvest.Features.Procedural.Systems
{
    /// <summary>
    /// Orchestrates asteroid field generation on scene load.
    /// Instantiates prefab entities with AsteroidComponent + AsteroidOreComponent.
    /// See MVP-07: Procedural asteroid field.
    /// </summary>
    [UpdateInGroup(typeof(InitializationSystemGroup))]
    public partial struct AsteroidFieldSystem : ISystem
    {
        private bool _generated;

        /// <summary>
        /// Initialize field generation flag. See MVP-07: Procedural asteroid field.
        /// </summary>
        public void OnCreate(ref SystemState state)
        {
            _generated = false;
            state.RequireForUpdate<AsteroidPrefabComponent>();
        }

        /// <summary>
        /// Generate asteroid field on first update, then disable. See MVP-07: Procedural asteroid field.
        /// </summary>
        public void OnUpdate(ref SystemState state)
        {
            if (_generated)
            {
                state.Enabled = false;
                return;
            }

            // Get prefab entity
            var prefabSingleton = SystemAPI.GetSingleton<AsteroidPrefabComponent>();
            var prefab = prefabSingleton.Prefab;

            var config = AsteroidFieldConfig.MvpDefault;
            int count = config.MaxAsteroids;

            // Allocate temp arrays
            var positions = new NativeArray<float3>(count, Allocator.TempJob);
            var oreTypeIds = new NativeArray<int>(count, Allocator.TempJob);
            var oreWeights = new NativeArray<float>(config.OreDistributions.Length, Allocator.TempJob);

            for (int i = 0; i < config.OreDistributions.Length; i++)
                oreWeights[i] = config.OreDistributions[i].Weight;

            // Run generator job
            var job = new AsteroidFieldGeneratorJob
            {
                Seed = config.Seed,
                MaxAsteroids = count,
                FieldRadius = config.FieldRadius,
                Positions = positions,
                OreTypeIds = oreTypeIds,
                OreWeights = oreWeights
            };
            job.Schedule(count, 64).Complete();

            // Instantiate prefab entities
            var rng = new Unity.Mathematics.Random(config.Seed);
            var em = state.EntityManager;

            for (int i = 0; i < count; i++)
            {
                var entity = em.Instantiate(prefab);

                float radius = rng.NextFloat(3f, 5f);
                float mass = radius * radius * radius * 10f;

                if (em.HasComponent<LocalTransform>(entity))
                    em.SetComponentData(entity, LocalTransform.FromPositionRotationScale(
                        positions[i], quaternion.identity, radius));
                else
                    em.AddComponentData(entity, LocalTransform.FromPositionRotationScale(
                        positions[i], quaternion.identity, radius));

                em.SetComponentData(entity, new AsteroidComponent
                {
                    Radius = radius,
                    InitialMass = mass,
                    RemainingMass = mass,
                    Depletion = 0f
                });

                em.SetComponentData(entity, new AsteroidOreComponent
                {
                    OreTypeId = oreTypeIds[i],
                    Quantity = mass,
                    Depth = rng.NextFloat(0f, 2f)
                });
            }

            // Cleanup
            positions.Dispose();
            oreTypeIds.Dispose();
            oreWeights.Dispose();

            _generated = true;
            state.Enabled = false;
            Debug.Log($"[VoidHarvest] AsteroidFieldSystem: Generated {count} asteroids in {config.FieldRadius}m radius.");
        }
    }
}
