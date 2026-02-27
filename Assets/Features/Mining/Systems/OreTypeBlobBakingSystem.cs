using Unity.Collections;
using Unity.Entities;
using VoidHarvest.Features.Mining.Data;

namespace VoidHarvest.Features.Mining.Systems
{
    /// <summary>
    /// Bakes OreTypeDefinition ScriptableObjects into an OreTypeBlobDatabase BlobAsset.
    /// Creates a singleton entity with OreTypeDatabaseComponent.
    /// Also maintains a managed string[] OreId lookup for MiningActionDispatchSystem.
    /// // CONSTITUTION DEVIATION: DOTS SystemBase uses static for managed data access
    /// See MVP-05: Mining beam and yield.
    /// </summary>
    [UpdateInGroup(typeof(InitializationSystemGroup))]
    public partial class OreTypeBlobBakingSystem : SystemBase
    {
        private static string[] _oreIdLookup;
        private bool _initialized;

        /// <summary>
        /// Set the OreTypeDefinition array to bake. Called from managed code during setup.
        /// </summary>
        public static void SetOreDefinitions(OreTypeDefinition[] definitions)
        {
            // Store definitions for OnUpdate to process
            _pendingDefinitions = definitions;
        }

        private static OreTypeDefinition[] _pendingDefinitions;

        /// <summary>
        /// Get the OreId string for an OreTypeId index.
        /// Used by MiningActionDispatchSystem to convert int OreTypeId back to string OreId.
        /// </summary>
        public static string GetOreId(int oreTypeId)
        {
            if (_oreIdLookup == null || oreTypeId < 0 || oreTypeId >= _oreIdLookup.Length)
                return "";
            return _oreIdLookup[oreTypeId];
        }

        protected override void OnUpdate()
        {
            if (_initialized)
            {
                Enabled = false;
                return;
            }

            // Wait until managed code provides the definitions
            if (_pendingDefinitions == null || _pendingDefinitions.Length == 0)
                return;

            var definitions = _pendingDefinitions;
            _oreIdLookup = new string[definitions.Length];

            // Build blob asset
            using var builder = new BlobBuilder(Allocator.Temp);
            ref var root = ref builder.ConstructRoot<OreTypeBlobDatabase>();
            var oreArray = builder.Allocate(ref root.OreTypes, definitions.Length);

            for (int i = 0; i < definitions.Length; i++)
            {
                var def = definitions[i];
                _oreIdLookup[i] = def.OreId;

                oreArray[i] = new OreTypeBlob
                {
                    BaseYieldPerSecond = def.BaseYieldPerSecond,
                    Hardness = def.Hardness,
                    Tier = def.Tier,
                    Rarity = def.Rarity,
                    VolumePerUnit = def.VolumePerUnit
                };
            }

            var blobRef = builder.CreateBlobAssetReference<OreTypeBlobDatabase>(Allocator.Persistent);

            // Create singleton entity
            var entity = EntityManager.CreateEntity();
            EntityManager.AddComponentData(entity, new OreTypeDatabaseComponent { Database = blobRef });
            EntityManager.AddComponentData(entity, new MiningActionBufferSingleton());

            _initialized = true;
            Enabled = false;

        }
    }
}
