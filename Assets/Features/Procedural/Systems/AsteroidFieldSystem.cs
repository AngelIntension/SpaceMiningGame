using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
using Unity.Rendering;
using Unity.Transforms;
using UnityEngine;
using UnityEngine.Rendering;
using VoidHarvest.Features.Mining.Data;
using VoidHarvest.Features.Procedural.Views;

namespace VoidHarvest.Features.Procedural.Systems
{
    /// <summary>
    /// Orchestrates asteroid field generation on scene load.
    /// Creates entities from scratch with RenderMeshUtility to ensure per-instance
    /// material property overrides (URPMaterialPropertyBaseColor) work correctly.
    /// Reads data-driven config from AsteroidFieldConfigComponent (baked by AsteroidFieldSpawner).
    /// See Spec 005: Data-Driven Ore System.
    /// </summary>
    [UpdateInGroup(typeof(InitializationSystemGroup))]
    public partial struct AsteroidFieldSystem : ISystem
    {
        private bool _generated;

        public void OnCreate(ref SystemState state)
        {
            _generated = false;
        }

        public void OnUpdate(ref SystemState state)
        {
            if (_generated)
            {
                state.Enabled = false;
                return;
            }

            var em = state.EntityManager;

            // Wait for mesh data from AsteroidPrefabAuthoring
            if (!SystemAPI.HasSingleton<AsteroidPrefabComponent>())
                return;

            // Data-driven path: process all AsteroidFieldConfigComponent entities
            bool hasFieldConfig = false;
            foreach (var (fieldConfig, weightBuffer, mappingBuf, entity)
                in SystemAPI.Query<
                    RefRO<AsteroidFieldConfigComponent>,
                    DynamicBuffer<AsteroidOreWeightElement>,
                    DynamicBuffer<AsteroidVisualMappingElement>>()
                .WithEntityAccess())
            {
                hasFieldConfig = true;
                GenerateField(ref state, em, fieldConfig.ValueRO, weightBuffer, mappingBuf, entity);
            }

            if (!hasFieldConfig)
                return; // Wait for data-driven config

            _generated = true;
            state.Enabled = false;
        }

        private void GenerateField(
            ref SystemState state,
            EntityManager em,
            AsteroidFieldConfigComponent config,
            DynamicBuffer<AsteroidOreWeightElement> weightBuffer,
            DynamicBuffer<AsteroidVisualMappingElement> mappingBuf,
            Entity configEntity)
        {
            int count = config.Count;
            if (count <= 0) return;

            // Copy buffers to NativeArrays (structural changes invalidate DynamicBuffer handles)
            var oreWeights = new NativeArray<float>(weightBuffer.Length, Allocator.TempJob);
            for (int i = 0; i < weightBuffer.Length; i++)
                oreWeights[i] = weightBuffer[i].NormalizedWeight;

            var mappingArray = new NativeArray<AsteroidVisualMappingElement>(mappingBuf.Length, Allocator.Temp);
            for (int i = 0; i < mappingBuf.Length; i++)
                mappingArray[i] = mappingBuf[i];

            // Load mesh prefab data from AsteroidPrefabComponent singleton entity
            var prefabSingletonEntity = SystemAPI.GetSingletonEntity<AsteroidPrefabComponent>();
            bool hasMeshVariants = em.HasBuffer<AsteroidMeshPrefabElement>(prefabSingletonEntity);

            NativeArray<AsteroidMeshPrefabElement> prefabArray;
            if (hasMeshVariants)
            {
                var buf = em.GetBuffer<AsteroidMeshPrefabElement>(prefabSingletonEntity);
                prefabArray = new NativeArray<AsteroidMeshPrefabElement>(buf.Length, Allocator.Temp);
                for (int i = 0; i < buf.Length; i++)
                    prefabArray[i] = buf[i];
            }
            else
            {
                prefabArray = new NativeArray<AsteroidMeshPrefabElement>(0, Allocator.Temp);
            }

            // Run generator job
            var positions = new NativeArray<float3>(count, Allocator.TempJob);
            var oreTypeIds = new NativeArray<int>(count, Allocator.TempJob);

            var job = new AsteroidFieldGeneratorJob
            {
                Seed = config.Seed,
                MaxAsteroids = count,
                FieldRadius = config.Radius,
                Positions = positions,
                OreTypeIds = oreTypeIds,
                OreWeights = oreWeights
            };
            job.Schedule(count, 64).Complete();

            bool useVisualMapping = prefabArray.Length > 0 && mappingArray.Length > 0;

            // Extract variant meshes/materials
            var renderMeshDesc = new RenderMeshDescription(ShadowCastingMode.On);
            Mesh[] variantMeshes = null;
            Material[] variantMaterials = null;
            float meshNormFactor = 1f;
            Mesh defaultMesh = null;
            Material defaultMaterial = null;

            if (useVisualMapping)
            {
                variantMeshes = new Mesh[prefabArray.Length];
                variantMaterials = new Material[prefabArray.Length];
                for (int i = 0; i < prefabArray.Length; i++)
                {
                    var prefabE = prefabArray[i].Prefab;
                    if (prefabE != Entity.Null && em.HasComponent<RenderMeshArray>(prefabE))
                    {
                        var rma = em.GetSharedComponentManaged<RenderMeshArray>(prefabE);
                        if (rma.MeshReferences != null && rma.MeshReferences.Length > 0)
                        {
                            int meshIdx = rma.MeshReferences.Length > 1 ? 1 : 0;
                            variantMeshes[i] = rma.MeshReferences[meshIdx].Value;

                            if (defaultMesh == null && variantMeshes[i] != null)
                            {
                                defaultMesh = variantMeshes[i];
                                var ext = defaultMesh.bounds.extents;
                                float maxExtent = math.max(ext.x, math.max(ext.y, ext.z));
                                if (maxExtent > 0.001f)
                                    meshNormFactor = 1f / maxExtent;
                            }
                        }
                        if (rma.MaterialReferences != null && rma.MaterialReferences.Length > 0)
                        {
                            variantMaterials[i] = rma.MaterialReferences[0].Value;
                            if (defaultMaterial == null)
                                defaultMaterial = variantMaterials[i];
                        }
                    }
                }
            }

            // Fallback: get default mesh from AsteroidPrefabComponent prefab entity
            if (defaultMesh == null)
            {
                var prefabSingleton = SystemAPI.GetSingleton<AsteroidPrefabComponent>();
                if (prefabSingleton.Prefab != Entity.Null && em.HasComponent<RenderMeshArray>(prefabSingleton.Prefab))
                {
                    var rma = em.GetSharedComponentManaged<RenderMeshArray>(prefabSingleton.Prefab);
                    if (rma.MeshReferences != null && rma.MeshReferences.Length > 0)
                    {
                        int meshIdx = rma.MeshReferences.Length > 1 ? 1 : 0;
                        defaultMesh = rma.MeshReferences[meshIdx].Value;
                        if (defaultMesh != null)
                        {
                            var ext = defaultMesh.bounds.extents;
                            float maxExtent = math.max(ext.x, math.max(ext.y, ext.z));
                            if (maxExtent > 0.001f)
                                meshNormFactor = 1f / maxExtent;
                        }
                    }
                    if (rma.MaterialReferences != null && rma.MaterialReferences.Length > 0)
                        defaultMaterial = rma.MaterialReferences[0].Value;
                }
            }

            var rmaCache = new System.Collections.Generic.Dictionary<(Mesh, Material), RenderMeshArray>();

            Debug.Log($"[VoidHarvest] AsteroidFieldSystem: meshNormFactor={meshNormFactor:F4}, " +
                $"useVisualMapping={useVisualMapping}, count={count}");

            var rng = new Unity.Mathematics.Random(config.Seed);

            for (int i = 0; i < count; i++)
            {
                int oreTypeId = oreTypeIds[i];
                Mesh mesh = defaultMesh;
                Material material = defaultMaterial;
                float4 pristineTintedColor = new float4(1f, 1f, 1f, 1f);

                if (useVisualMapping && oreTypeId >= 0 && oreTypeId < mappingArray.Length)
                {
                    var mapping = mappingArray[oreTypeId];

                    int variantChoice = AsteroidVisualMappingHelper.SelectMeshVariant(positions[i]);
                    int primaryIdx = variantChoice == 0 ? mapping.MeshVariantAIndex : mapping.MeshVariantBIndex;
                    int fallbackIdx = variantChoice == 0 ? mapping.MeshVariantBIndex : mapping.MeshVariantAIndex;

                    if (primaryIdx >= 0 && primaryIdx < variantMeshes.Length &&
                        variantMeshes[primaryIdx] != null)
                    {
                        mesh = variantMeshes[primaryIdx];
                        material = variantMaterials[primaryIdx];
                    }
                    else if (fallbackIdx >= 0 && fallbackIdx < variantMeshes.Length &&
                        variantMeshes[fallbackIdx] != null)
                    {
                        mesh = variantMeshes[fallbackIdx];
                        material = variantMaterials[fallbackIdx];
                    }

                    pristineTintedColor = new float4(
                        AsteroidVisualMappingHelper.PristineGray * mapping.TintColor.x,
                        AsteroidVisualMappingHelper.PristineGray * mapping.TintColor.y,
                        AsteroidVisualMappingHelper.PristineGray * mapping.TintColor.z,
                        1f);
                }

                if (mesh == null || material == null)
                    continue;

                var key = (mesh, material);
                if (!rmaCache.TryGetValue(key, out var entityRma))
                {
                    entityRma = new RenderMeshArray(new[] { material }, new[] { mesh });
                    rmaCache[key] = entityRma;
                }

                var entity = em.CreateEntity();
                RenderMeshUtility.AddComponents(entity, em, renderMeshDesc, entityRma,
                    MaterialMeshInfo.FromRenderMeshArrayIndices(0, 0));

                float radius = rng.NextFloat(config.SizeMin, config.SizeMax);
                float mass = radius * radius * radius * 10f;
                float entityScale = radius * meshNormFactor;

                var localTransform = LocalTransform.FromPositionRotationScale(
                    positions[i], quaternion.identity, entityScale);

                if (em.HasComponent<LocalTransform>(entity))
                    em.SetComponentData(entity, localTransform);
                else
                    em.AddComponentData(entity, localTransform);

                em.SetComponentData(entity, new LocalToWorld
                {
                    Value = localTransform.ToMatrix()
                });

                em.AddComponentData(entity, new AsteroidComponent
                {
                    Radius = radius,
                    InitialMass = mass,
                    RemainingMass = mass,
                    Depletion = 0f,
                    PristineTintedColor = pristineTintedColor,
                    CrumbleThresholdsPassed = 0,
                    CrumblePauseTimer = 0f,
                    FadeOutTimer = 0f,
                    MeshNormFactor = meshNormFactor
                });

                em.AddComponentData(entity, new AsteroidOreComponent
                {
                    OreTypeId = oreTypeId,
                    Quantity = mass,
                    Depth = rng.NextFloat(0f, 2f)
                });

                em.AddComponentData(entity, new URPMaterialPropertyBaseColor
                {
                    Value = pristineTintedColor
                });

                em.AddComponentData(entity, new AsteroidEmissionComponent
                {
                    Value = new float4(0f, 0f, 0f, 0f)
                });

                em.AddComponentData(entity, new AsteroidGlowFadeComponent { Value = 0f });
            }

            positions.Dispose();
            oreTypeIds.Dispose();
            oreWeights.Dispose();
            prefabArray.Dispose();
            mappingArray.Dispose();

            string mode = useVisualMapping ? "visual mapping" : "single-prefab";
            Debug.Log($"[VoidHarvest] AsteroidFieldSystem: Generated {count} asteroids in {config.Radius}m radius ({mode} mode).");
        }
    }
}
