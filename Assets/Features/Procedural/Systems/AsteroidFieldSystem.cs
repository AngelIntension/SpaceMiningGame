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
    /// Supports multi-prefab mode (premium visual mapping) and single-prefab (backward compatible).
    /// See MVP-07: Procedural asteroid field, FR-006: Ore-to-mesh mapping, FR-008: Ore tint.
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
        /// Generate asteroid field on first update, then disable.
        /// See MVP-07: Procedural asteroid field, FR-006: Visual mapping.
        /// </summary>
        public void OnUpdate(ref SystemState state)
        {
            if (_generated)
            {
                state.Enabled = false;
                return;
            }

            // Get prefab singleton
            var prefabSingleton = SystemAPI.GetSingleton<AsteroidPrefabComponent>();
            var defaultPrefab = prefabSingleton.Prefab;
            var prefabSingletonEntity = SystemAPI.GetSingletonEntity<AsteroidPrefabComponent>();

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

            // Check for multi-prefab mode
            var em = state.EntityManager;
            bool hasVariants = em.HasBuffer<AsteroidMeshPrefabElement>(prefabSingletonEntity);
            bool hasMapping = em.HasBuffer<AsteroidVisualMappingElement>(prefabSingletonEntity);

            DynamicBuffer<AsteroidMeshPrefabElement> prefabBuffer = default;
            DynamicBuffer<AsteroidVisualMappingElement> mappingBuffer = default;

            if (hasVariants)
                prefabBuffer = em.GetBuffer<AsteroidMeshPrefabElement>(prefabSingletonEntity);
            if (hasMapping)
                mappingBuffer = em.GetBuffer<AsteroidVisualMappingElement>(prefabSingletonEntity);

            bool useVisualMapping = hasVariants && hasMapping && prefabBuffer.Length > 0 && mappingBuffer.Length > 0;

            // Instantiate prefab entities
            var rng = new Unity.Mathematics.Random(config.Seed);

            for (int i = 0; i < count; i++)
            {
                int oreTypeId = oreTypeIds[i];
                Entity prefab = defaultPrefab;
                float4 pristineTintedColor = new float4(0.314f, 0.314f, 0.314f, 1f);

                if (useVisualMapping && oreTypeId >= 0 && oreTypeId < mappingBuffer.Length)
                {
                    var mapping = mappingBuffer[oreTypeId];

                    // Select mesh variant via position hash (FR-007 cluster variety)
                    int variantChoice = AsteroidVisualMappingHelper.SelectMeshVariant(positions[i]);

                    int primaryIdx = variantChoice == 0 ? mapping.MeshVariantAIndex : mapping.MeshVariantBIndex;
                    int fallbackIdx = variantChoice == 0 ? mapping.MeshVariantBIndex : mapping.MeshVariantAIndex;

                    // EC3: null mesh fallback — try primary, then fallback, then default
                    Entity variantPrefab = Entity.Null;
                    if (primaryIdx >= 0 && primaryIdx < prefabBuffer.Length)
                        variantPrefab = prefabBuffer[primaryIdx].Prefab;

                    if (variantPrefab == Entity.Null && fallbackIdx >= 0 && fallbackIdx < prefabBuffer.Length)
                        variantPrefab = prefabBuffer[fallbackIdx].Prefab;

                    if (variantPrefab != Entity.Null)
                        prefab = variantPrefab;

                    // Calculate PristineTintedColor from ore tint (FR-008)
                    pristineTintedColor = new float4(
                        AsteroidVisualMappingHelper.PristineGray * mapping.TintColor.x,
                        AsteroidVisualMappingHelper.PristineGray * mapping.TintColor.y,
                        AsteroidVisualMappingHelper.PristineGray * mapping.TintColor.z,
                        1f);
                }

                var entity = em.Instantiate(prefab);

                float radius = rng.NextFloat(3f, 5f);
                float mass = radius * radius * radius * 10f;

                if (em.HasComponent<LocalTransform>(entity))
                    em.SetComponentData(entity, LocalTransform.FromPositionRotationScale(
                        positions[i], quaternion.identity, radius));
                else
                    em.AddComponentData(entity, LocalTransform.FromPositionRotationScale(
                        positions[i], quaternion.identity, radius));

                // Set asteroid data with new depletion visual fields initialized (T018, T022)
                em.SetComponentData(entity, new AsteroidComponent
                {
                    Radius = radius,
                    InitialMass = mass,
                    RemainingMass = mass,
                    Depletion = 0f,
                    PristineTintedColor = pristineTintedColor,
                    CrumbleThresholdsPassed = 0,
                    CrumblePauseTimer = 0f,
                    FadeOutTimer = 0f
                });

                em.SetComponentData(entity, new AsteroidOreComponent
                {
                    OreTypeId = oreTypeId,
                    Quantity = mass,
                    Depth = rng.NextFloat(0f, 2f)
                });

                // Set initial base color to the pristine tinted color
                if (em.HasComponent<AsteroidBaseColorOverride>(entity))
                {
                    em.SetComponentData(entity, new AsteroidBaseColorOverride
                    {
                        Value = pristineTintedColor
                    });
                }
            }

            // Cleanup
            positions.Dispose();
            oreTypeIds.Dispose();
            oreWeights.Dispose();

            _generated = true;
            state.Enabled = false;

            string mode = useVisualMapping ? "visual mapping" : "single-prefab";
            Debug.Log($"[VoidHarvest] AsteroidFieldSystem: Generated {count} asteroids in {config.FieldRadius}m radius ({mode} mode).");
        }
    }
}
