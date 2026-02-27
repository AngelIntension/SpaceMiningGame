using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;
using VoidHarvest.Features.Mining.Data;

namespace VoidHarvest.Features.Procedural.Views
{
    /// <summary>
    /// Authoring component for the asteroid prefab entity.
    /// Place on a GameObject in a SubScene with MeshFilter + MeshRenderer.
    /// The baker creates a prefab entity that AsteroidFieldSystem instantiates.
    /// See MVP-07: Procedural asteroid field.
    /// </summary>
    public class AsteroidPrefabAuthoring : MonoBehaviour
    {
    }

    /// <summary>
    /// Singleton component holding the asteroid prefab entity reference.
    /// </summary>
    public struct AsteroidPrefabComponent : IComponentData
    {
        public Entity Prefab;
    }

    public class AsteroidPrefabBaker : Baker<AsteroidPrefabAuthoring>
    {
        public override void Bake(AsteroidPrefabAuthoring authoring)
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
        }
    }
}
