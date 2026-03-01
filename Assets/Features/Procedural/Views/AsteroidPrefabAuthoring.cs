using Unity.Entities;
using Unity.Mathematics;
using Unity.Rendering;
using UnityEngine;
using VoidHarvest.Features.Mining.Data;

namespace VoidHarvest.Features.Procedural.Views
{
    /// <summary>
    /// Authoring component for asteroid prefab entities.
    /// Supports two modes:
    /// 1. Single-prefab (backward compatible): Place on a GO with MeshFilter + MeshRenderer.
    /// 2. Multi-prefab: Reference mesh variant GameObjects for per-ore visual variety.
    /// The baker creates prefab entities that AsteroidFieldSystem uses for mesh/material data.
    /// See Spec 005: Data-Driven Ore System, FR-006: Ore-to-mesh mapping.
    /// </summary>
    public class AsteroidPrefabAuthoring : MonoBehaviour
    {
        /// <summary>
        /// Optional: mesh variant prefab GameObjects. Each should have MeshFilter + MeshRenderer
        /// and an AsteroidVariantAuthoring component. Order: 2 per ore type (VariantA, VariantB).
        /// Leave empty for single-prefab backward compatibility.
        /// See FR-006: Ore-to-mesh mapping.
        /// </summary>
        [Tooltip("Mesh variant prefabs (2 per ore type). Leave empty for single-prefab mode.")]
        public GameObject[] MeshVariantPrefabs;
    }

    /// <summary>
    /// Singleton component holding the default asteroid prefab entity reference.
    /// See Spec 005.
    /// </summary>
    public struct AsteroidPrefabComponent : IComponentData
    {
        public Entity Prefab;
    }

    /// <summary>
    /// Buffer element holding a mesh variant prefab entity reference.
    /// Order matches AsteroidPrefabAuthoring.MeshVariantPrefabs:
    /// [0]=OreType0_VariantA, [1]=OreType0_VariantB, [2]=OreType1_VariantA, etc.
    /// See FR-006: Ore-to-mesh mapping.
    /// </summary>
    public struct AsteroidMeshPrefabElement : IBufferElementData
    {
        /// <summary>Prefab entity for this mesh variant.</summary>
        public Entity Prefab;
    }

    /// <summary>
    /// Buffer element holding baked visual mapping data for each ore type.
    /// Order matches OreEntries in AsteroidFieldDefinition.
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
            // so Entities Graphics sets up per-instance property uploads
            AddComponent(prefabEntity, new URPMaterialPropertyBaseColor
                { Value = new float4(1f, 1f, 1f, 1f) });

            // Create a singleton entity to hold the prefab reference
            var singletonEntity = CreateAdditionalEntity(TransformUsageFlags.None, entityName: "AsteroidPrefabSingleton");
            AddComponent(singletonEntity, new AsteroidPrefabComponent { Prefab = prefabEntity });
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
                    prefabEntity = GetEntity(go, TransformUsageFlags.Dynamic);
                    if (firstValidPrefab == Entity.Null)
                        firstValidPrefab = prefabEntity;
                }
                else
                {
                    prefabEntity = Entity.Null;
                }

                prefabBuffer.Add(new AsteroidMeshPrefabElement { Prefab = prefabEntity });
            }

            // First valid variant is the default prefab
            AddComponent(singletonEntity, new AsteroidPrefabComponent
            {
                Prefab = firstValidPrefab
            });
        }
    }
}
