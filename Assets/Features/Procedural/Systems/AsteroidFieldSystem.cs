using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
using Unity.Rendering;
using Unity.Transforms;
using UnityEngine;
using UnityEngine.Rendering;
using VoidHarvest.Features.Mining.Data;
using VoidHarvest.Features.Procedural.Data;
using VoidHarvest.Features.Procedural.Views;

namespace VoidHarvest.Features.Procedural.Systems
{
    /// <summary>
    /// Orchestrates asteroid field generation on scene load.
    /// Creates entities from scratch with RenderMeshUtility to ensure per-instance
    /// material property overrides (URPMaterialPropertyBaseColor) work correctly.
    /// Supports multi-prefab mode (premium visual mapping) and single-prefab (backward compatible).
    /// See MVP-07: Procedural asteroid field, FR-006: Ore-to-mesh mapping, FR-008: Ore tint.
    /// </summary>
    [UpdateInGroup(typeof(InitializationSystemGroup))]
    public partial struct AsteroidFieldSystem : ISystem
    {
        private bool _generated;

        public void OnCreate(ref SystemState state)
        {
            _generated = false;
            state.RequireForUpdate<AsteroidPrefabComponent>();
        }

        public void OnUpdate(ref SystemState state)
        {
            if (_generated)
            {
                state.Enabled = false;
                return;
            }

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

            // Check for multi-prefab mode — copy buffer data into NativeArrays before
            // the instantiation loop, since structural changes invalidate DynamicBuffer handles.
            var em = state.EntityManager;
            bool hasVariants = em.HasBuffer<AsteroidMeshPrefabElement>(prefabSingletonEntity);
            bool hasMapping = em.HasBuffer<AsteroidVisualMappingElement>(prefabSingletonEntity);

            var prefabArray = new NativeArray<AsteroidMeshPrefabElement>(0, Allocator.Temp);
            var mappingArray = new NativeArray<AsteroidVisualMappingElement>(0, Allocator.Temp);

            if (hasVariants)
            {
                var buf = em.GetBuffer<AsteroidMeshPrefabElement>(prefabSingletonEntity);
                prefabArray = new NativeArray<AsteroidMeshPrefabElement>(buf.Length, Allocator.Temp);
                for (int i = 0; i < buf.Length; i++)
                    prefabArray[i] = buf[i];
            }
            if (hasMapping)
            {
                var buf = em.GetBuffer<AsteroidVisualMappingElement>(prefabSingletonEntity);
                mappingArray = new NativeArray<AsteroidVisualMappingElement>(buf.Length, Allocator.Temp);
                for (int i = 0; i < buf.Length; i++)
                    mappingArray[i] = buf[i];
            }

            bool useVisualMapping = hasVariants && hasMapping && prefabArray.Length > 0 && mappingArray.Length > 0;

            // Pre-extract mesh/material from each variant prefab's RenderMeshArray.
            // We create entities from scratch via RenderMeshUtility.AddComponents (not em.Instantiate)
            // because SubScene-baked prefab instantiation does not support per-instance material
            // property overrides (URPMaterialPropertyBaseColor) — the batch metadata from baking
            // doesn't register the override component for GPU upload.
            var renderMeshDesc = new RenderMeshDescription(ShadowCastingMode.On);

            // Extract default prefab's mesh and material
            Mesh defaultMesh = null;
            Material defaultMaterial = null;
            float meshNormFactor = 1f;

            if (em.HasComponent<RenderMeshArray>(defaultPrefab))
            {
                var rma = em.GetSharedComponentManaged<RenderMeshArray>(defaultPrefab);
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
                if (rma.Materials != null && rma.Materials.Length > 0)
                    defaultMaterial = rma.Materials[0];
            }

            // Extract variant meshes/materials for multi-prefab mode
            Mesh[] variantMeshes = null;
            Material[] variantMaterials = null;

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
                        }
                        if (rma.Materials != null && rma.Materials.Length > 0)
                            variantMaterials[i] = rma.Materials[0];
                    }
                }
            }

            // Pre-create RenderMeshArray per unique mesh+material combo for batch efficiency.
            // Entities sharing the same RenderMeshArray are grouped into the same rendering batch.
            var rmaCache = new System.Collections.Generic.Dictionary<(Mesh, Material), RenderMeshArray>();

            Debug.Log($"[VoidHarvest] AsteroidFieldSystem: meshNormFactor={meshNormFactor:F4}, " +
                $"useVisualMapping={useVisualMapping}");

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

                    // EC3: null mesh fallback — try primary, then fallback, then default
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

                // Get or create cached RenderMeshArray for this mesh+material combo
                var key = (mesh, material);
                if (!rmaCache.TryGetValue(key, out var entityRma))
                {
                    entityRma = new RenderMeshArray(new[] { material }, new[] { mesh });
                    rmaCache[key] = entityRma;
                }

                // Create entity from scratch with RenderMeshUtility for proper per-instance
                // material property override support
                var entity = em.CreateEntity();
                RenderMeshUtility.AddComponents(entity, em, renderMeshDesc, entityRma,
                    MaterialMeshInfo.FromRenderMeshArrayIndices(0, 0));

                float radius = rng.NextFloat(3f, 5f);
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
            }

            // Cleanup
            positions.Dispose();
            oreTypeIds.Dispose();
            oreWeights.Dispose();
            prefabArray.Dispose();
            mappingArray.Dispose();

            _generated = true;
            state.Enabled = false;

            string mode = useVisualMapping ? "visual mapping" : "single-prefab";
            Debug.Log($"[VoidHarvest] AsteroidFieldSystem: Generated {count} asteroids in {config.FieldRadius}m radius ({mode} mode).");
        }
    }
}
