using Unity.Collections;
using Unity.Entities;
using VoidHarvest.Features.Mining.Data;

namespace VoidHarvest.Features.Mining.Systems
{
    /// <summary>
    /// Bakes OreDefinition ScriptableObjects into an OreTypeBlobDatabase BlobAsset.
    /// Creates a singleton entity with OreTypeDatabaseComponent.
    /// Also maintains a managed string[] OreId lookup for MiningActionDispatchSystem.
    /// // CONSTITUTION DEVIATION: DOTS SystemBase uses static for managed data access
    /// See Spec 005: Data-Driven Ore System.
    /// </summary>
    [UpdateInGroup(typeof(InitializationSystemGroup))]
    public partial class OreTypeBlobBakingSystem : SystemBase
    {
        private static string[] _oreIdLookup;
        private bool _initialized;
        private BlobAssetReference<OreTypeBlobDatabase> _blobRef;

        /// <summary>
        /// Set the OreDefinition array to bake. Called from managed code during setup.
        /// Immediately populates the OreId lookup and OreDisplayNames registry.
        /// </summary>
        public static void SetOreDefinitions(OreDefinition[] definitions)
        {
            _pendingDefinitions = definitions;

            // Populate lookups immediately so GetOreId()/OreDisplayNames.Get() are available
            if (definitions != null && definitions.Length > 0)
            {
                _oreIdLookup = new string[definitions.Length];
                var displayNames = new string[definitions.Length];
                var oreIdToDisplayName = new System.Collections.Generic.Dictionary<string, string>(definitions.Length);
                for (int i = 0; i < definitions.Length; i++)
                {
                    var def = definitions[i];
                    string oreId = def != null ? def.OreId : "";
                    string displayName = def != null && !string.IsNullOrEmpty(def.DisplayName) ? def.DisplayName : oreId;
                    _oreIdLookup[i] = oreId;
                    displayNames[i] = displayName;
                    if (!string.IsNullOrEmpty(oreId))
                        oreIdToDisplayName[oreId] = displayName;
                }
                OreDisplayNames.SetLookups(displayNames, oreIdToDisplayName);
                OreDefinitionRegistry.SetDefinitions(definitions);
            }
        }

        private static OreDefinition[] _pendingDefinitions;

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

            // Build blob asset
            using var builder = new BlobBuilder(Allocator.Temp);
            ref var root = ref builder.ConstructRoot<OreTypeBlobDatabase>();
            var oreArray = builder.Allocate(ref root.OreTypes, definitions.Length);

            for (int i = 0; i < definitions.Length; i++)
            {
                var def = definitions[i];
                oreArray[i] = new OreTypeBlob
                {
                    BaseYieldPerSecond = def.BaseYieldPerSecond,
                    Hardness = def.Hardness,
                    VolumePerUnit = def.VolumePerUnit
                };
            }

            _blobRef = builder.CreateBlobAssetReference<OreTypeBlobDatabase>(Allocator.Persistent);

            // Create singleton entity
            var entity = EntityManager.CreateEntity();
            EntityManager.AddComponentData(entity, new OreTypeDatabaseComponent { Database = _blobRef });
            EntityManager.AddComponentData(entity, new MiningActionBufferSingleton());

            _initialized = true;
            Enabled = false;

        }

        protected override void OnDestroy()
        {
            if (_blobRef.IsCreated)
                _blobRef.Dispose();
        }
    }
}
