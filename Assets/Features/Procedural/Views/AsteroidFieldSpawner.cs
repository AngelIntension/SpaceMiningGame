using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;
using VoidHarvest.Features.Mining.Data;
using VoidHarvest.Features.Procedural.Data;

namespace VoidHarvest.Features.Procedural.Views
{
    /// <summary>
    /// Authoring component that references an AsteroidFieldDefinition and bakes
    /// spawn configuration into ECS. Place on a GameObject in a SubScene.
    /// Multiple spawners can exist in a scene for distinct asteroid fields.
    /// See Spec 005: Data-Driven Ore System.
    /// </summary>
    public class AsteroidFieldSpawner : MonoBehaviour
    {
        /// <summary>Reference to the field configuration ScriptableObject.</summary>
        [Tooltip("Asteroid field configuration asset.")]
        public AsteroidFieldDefinition FieldDefinition;
    }

    /// <summary>
    /// Baked spatial configuration for an asteroid field.
    /// See Spec 005: Data-Driven Ore System.
    /// </summary>
    public struct AsteroidFieldConfigComponent : IComponentData
    {
        public int Count;
        public float Radius;
        public uint Seed;
        public float SizeMin;
        public float SizeMax;
        public float RotationMin;
        public float RotationMax;
    }

    /// <summary>
    /// Buffer element holding normalized ore weight for an asteroid field entry.
    /// Index matches OreEntries order in AsteroidFieldDefinition.
    /// See Spec 005: Data-Driven Ore System.
    /// </summary>
    public struct AsteroidOreWeightElement : IBufferElementData
    {
        /// <summary>Normalized spawn probability for this ore type.</summary>
        public float NormalizedWeight;
        /// <summary>Index into the ore type database (OreTypeBlobDatabase).</summary>
        public int OreTypeIndex;
    }

    /// <summary>
    /// Baker for AsteroidFieldSpawner. Bakes field config, ore weights,
    /// and visual mapping into ECS components.
    /// </summary>
    public class AsteroidFieldSpawnerBaker : Baker<AsteroidFieldSpawner>
    {
        public override void Bake(AsteroidFieldSpawner authoring)
        {
            if (authoring.FieldDefinition == null)
            {
                Debug.LogWarning("[VoidHarvest] AsteroidFieldSpawner: No FieldDefinition assigned.");
                return;
            }

            var def = authoring.FieldDefinition;
            var entity = GetEntity(TransformUsageFlags.None);

            // Bake spatial config
            AddComponent(entity, new AsteroidFieldConfigComponent
            {
                Count = def.AsteroidCount,
                Radius = def.FieldRadius,
                Seed = def.Seed,
                SizeMin = def.AsteroidSizeMin,
                SizeMax = def.AsteroidSizeMax,
                RotationMin = def.RotationSpeedMin,
                RotationMax = def.RotationSpeedMax
            });

            // Bake MinScaleFraction
            AddComponent(entity, new AsteroidVisualMappingSingleton
            {
                MinScaleFraction = def.MinScaleFraction
            });

            // Normalize weights and bake ore weight buffer
            var normalizedWeights = AsteroidFieldDefinition.NormalizeWeights(def.OreEntries);
            var weightBuffer = AddBuffer<AsteroidOreWeightElement>(entity);

            for (int i = 0; i < def.OreEntries.Length; i++)
            {
                weightBuffer.Add(new AsteroidOreWeightElement
                {
                    NormalizedWeight = i < normalizedWeights.Length ? normalizedWeights[i] : 0f,
                    OreTypeIndex = i
                });
            }

            // Bake visual mapping (tint colors + mesh variant indices)
            var mappingBuffer = AddBuffer<AsteroidVisualMappingElement>(entity);

            for (int i = 0; i < def.OreEntries.Length; i++)
            {
                var entry = def.OreEntries[i];
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
