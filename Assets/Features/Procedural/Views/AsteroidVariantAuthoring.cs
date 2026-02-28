using Unity.Entities;
using Unity.Mathematics;
using Unity.Rendering;
using UnityEngine;
using VoidHarvest.Features.Mining.Data;
using VoidHarvest.Features.Procedural.Data;

namespace VoidHarvest.Features.Procedural.Views
{
    /// <summary>
    /// Place on each mesh variant GameObject in the SubScene to mark it as an asteroid
    /// prefab variant. The baker adds the Prefab tag and asteroid ECS components.
    /// Must be in a separate file from AsteroidPrefabAuthoring to get its own script GUID,
    /// ensuring Unity's DOTS baking system runs the correct Baker per authoring type.
    /// See FR-006: Ore-to-mesh mapping.
    /// </summary>
    public class AsteroidVariantAuthoring : MonoBehaviour
    {
    }

    public class AsteroidVariantBaker : Baker<AsteroidVariantAuthoring>
    {
        public override void Bake(AsteroidVariantAuthoring authoring)
        {
            var entity = GetEntity(TransformUsageFlags.Dynamic);

            // Mark as prefab — excluded from queries and rendering until instantiated
            AddComponent<Prefab>(entity);

            // Add asteroid-specific components with defaults (overwritten at spawn time)
            AddComponent(entity, new AsteroidComponent());
            AddComponent(entity, new AsteroidOreComponent());

            // Material property override for depletion visual
            AddComponent(entity, new URPMaterialPropertyBaseColor
                { Value = new float4(1f, 1f, 1f, 1f) });
        }
    }
}
