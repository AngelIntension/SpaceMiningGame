using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;
using VoidHarvest.Features.Mining.Data;
using VoidHarvest.Features.Procedural.Data;

namespace VoidHarvest.Features.Procedural.Views
{
    /// <summary>
    /// Authoring component for asteroid prefab entities.
    /// Supports two modes:
    /// 1. Single-prefab (backward compatible): Place on a GO with MeshFilter + MeshRenderer.
    /// 2. Multi-prefab: Reference mesh variant GameObjects and a visual mapping config.
    /// The baker creates prefab entities that AsteroidFieldSystem instantiates.
    /// See MVP-07: Procedural asteroid field, FR-006: Ore-to-mesh mapping.
    /// </summary>
    public class AsteroidPrefabAuthoring : MonoBehaviour
    {
        /// <summary>
        /// Optional: mesh variant prefab GameObjects. Each should have MeshFilter + MeshRenderer
        /// and an AsteroidVariantAuthoring component. Order: VeldsparA, VeldsparB, ScorditeA,
        /// ScorditeB, PyroxeresA, PyroxeresB. Leave empty for single-prefab backward compatibility.
        /// See FR-006: Ore-to-mesh mapping.
        /// </summary>
        [Tooltip("Mesh variant prefabs (6 total: 2 per ore type). Leave empty for single-prefab mode.")]
        public GameObject[] MeshVariantPrefabs;

        /// <summary>
        /// Optional: visual mapping config defining ore→tint mapping and MinScaleFraction.
        /// Required when using multi-prefab mode.
        /// See FR-006, FR-008, FR-019.
        /// </summary>
        [Tooltip("Visual mapping config (required for multi-prefab mode).")]
        public AsteroidVisualMappingConfig VisualMappingConfig;
    }

    /// <summary>
    /// Singleton component holding the default asteroid prefab entity reference.
    /// Backward compatible with single-prefab mode.
    /// See MVP-07.
    /// </summary>
    public struct AsteroidPrefabComponent : IComponentData
    {
        public Entity Prefab;
    }

    /// <summary>
    /// Buffer element holding a mesh variant prefab entity reference.
    /// Order matches AsteroidPrefabAuthoring.MeshVariantPrefabs:
    /// [0]=VeldsparA, [1]=VeldsparB, [2]=ScorditeA, etc.
    /// See FR-006: Ore-to-mesh mapping.
    /// </summary>
    public struct AsteroidMeshPrefabElement : IBufferElementData
    {
        /// <summary>Prefab entity for this mesh variant.</summary>
        public Entity Prefab;
    }

    /// <summary>
    /// Buffer element holding baked visual mapping data for each ore type.
    /// Order matches AsteroidVisualMappingConfig.Entries.
    /// See FR-006, FR-008.
    /// </summary>
    public struct AsteroidVisualMappingElement : IBufferElementData
    {
        /// <summary>Tint color for this ore type (from config). See FR-008.</summary>
        public float4 TintColor;
        /// <summary>Index into AsteroidMeshPrefabElement buffer for variant A.</summary>
        public int MeshVariantAIndex;
        /// <summary>Index into AsteroidMeshPrefabElement buffer for variant B.</summary>
        public int MeshVariantBIndex;
    }

    /// <summary>
    /// Place on each mesh variant GameObject in the SubScene to mark it as an asteroid
    /// prefab variant. The baker adds the Prefab tag and asteroid ECS components.
    /// See FR-006: Ore-to-mesh mapping.
    /// </summary>
    public class AsteroidVariantAuthoring : MonoBehaviour
    {
    }

    public class AsteroidVariantBaker : Baker<AsteroidVariantAuthoring>
    {
        public override void Bake(AsteroidVariantAuthoring authoring)
        {
            var entity = GetEntity(TransformUsageFlags.Renderable);

            // Mark as prefab — excluded from queries and rendering until instantiated
            AddComponent<Prefab>(entity);

            // Add asteroid-specific components with defaults (overwritten at spawn time)
            AddComponent(entity, new AsteroidComponent());
            AddComponent(entity, new AsteroidOreComponent());

            // Material property override for depletion visual
            AddComponent(entity, new AsteroidBaseColorOverride
                { Value = new float4(0.314f, 0.314f, 0.314f, 1f) });
        }
    }

    public class AsteroidPrefabBaker : Baker<AsteroidPrefabAuthoring>
    {
        public override void Bake(AsteroidPrefabAuthoring authoring)
        {
            bool hasVariants = authoring.MeshVariantPrefabs != null
                && authoring.MeshVariantPrefabs.Length > 0;

            if (hasVariants)
            {
                BakeMultiPrefab(authoring);
            }
            else
            {
                BakeSinglePrefab(authoring);
            }
        }

        private void BakeSinglePrefab(AsteroidPrefabAuthoring authoring)
        {
            var prefabEntity = GetEntity(TransformUsageFlags.Renderable);

            // Mark as prefab — excluded from queries and rendering until instantiated
            AddComponent<Prefab>(prefabEntity);

            // Add asteroid-specific components with defaults (overwritten at spawn time)
            AddComponent(prefabEntity, new AsteroidComponent());
            AddComponent(prefabEntity, new AsteroidOreComponent());

            // Material property override for depletion visual — must be on prefab archetype
            // so Entities Graphics sets up per-instance property uploads (MVP-07)
            AddComponent(prefabEntity, new AsteroidBaseColorOverride
                { Value = new float4(0.314f, 0.314f, 0.314f, 1f) });

            // Create a singleton entity to hold the prefab reference
            var singletonEntity = CreateAdditionalEntity(TransformUsageFlags.None, entityName: "AsteroidPrefabSingleton");
            AddComponent(singletonEntity, new AsteroidPrefabComponent { Prefab = prefabEntity });

            // Bake MinScaleFraction if config is available
            if (authoring.VisualMappingConfig != null)
            {
                AddComponent(singletonEntity, new AsteroidVisualMappingSingleton
                {
                    MinScaleFraction = authoring.VisualMappingConfig.MinScaleFraction
                });
            }
        }

        private void BakeMultiPrefab(AsteroidPrefabAuthoring authoring)
        {
            var singletonEntity = CreateAdditionalEntity(TransformUsageFlags.None, entityName: "AsteroidPrefabSingleton");

            // Build prefab buffer from variant references
            var prefabBuffer = AddBuffer<AsteroidMeshPrefabElement>(singletonEntity);
            Entity firstValidPrefab = Entity.Null;

            for (int i = 0; i < authoring.MeshVariantPrefabs.Length; i++)
            {
                var go = authoring.MeshVariantPrefabs[i];
                Entity prefabEntity;

                if (go != null)
                {
                    prefabEntity = GetEntity(go, TransformUsageFlags.Renderable);
                    if (firstValidPrefab == Entity.Null)
                        firstValidPrefab = prefabEntity;
                }
                else
                {
                    prefabEntity = Entity.Null;
                }

                prefabBuffer.Add(new AsteroidMeshPrefabElement { Prefab = prefabEntity });
            }

            // Backward-compatible: first valid variant is the default prefab
            AddComponent(singletonEntity, new AsteroidPrefabComponent
            {
                Prefab = firstValidPrefab
            });

            // Bake visual mapping data from config
            if (authoring.VisualMappingConfig != null)
            {
                var config = authoring.VisualMappingConfig;

                AddComponent(singletonEntity, new AsteroidVisualMappingSingleton
                {
                    MinScaleFraction = config.MinScaleFraction
                });

                var mappingBuffer = AddBuffer<AsteroidVisualMappingElement>(singletonEntity);
                for (int i = 0; i < config.Entries.Length; i++)
                {
                    var entry = config.Entries[i];
                    mappingBuffer.Add(new AsteroidVisualMappingElement
                    {
                        TintColor = new float4(entry.TintColor.r, entry.TintColor.g, entry.TintColor.b, entry.TintColor.a),
                        MeshVariantAIndex = i * 2,
                        MeshVariantBIndex = i * 2 + 1
                    });
                }
            }
        }
    }
}
