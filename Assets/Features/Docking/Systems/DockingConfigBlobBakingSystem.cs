using Unity.Entities;
using VoidHarvest.Features.Docking.Data;

namespace VoidHarvest.Features.Docking.Systems
{
    /// <summary>
    /// Bakes DockingConfig ScriptableObject into a DockingConfigBlob BlobAsset.
    /// Creates a singleton entity with DockingConfigBlobComponent.
    /// Follows OreTypeBlobBakingSystem pattern: static Set → OnUpdate blob build → singleton → self-disable.
    /// // CONSTITUTION DEVIATION: DOTS SystemBase uses static for managed data bridge
    /// See Spec 009: Data-Driven World Config (US2).
    /// </summary>
    [UpdateInGroup(typeof(InitializationSystemGroup))]
    public partial class DockingConfigBlobBakingSystem : SystemBase
    {
        private static DockingConfig _pendingConfig;
        private bool _initialized;
        private BlobAssetReference<DockingConfigBlob> _blobRef;

        /// <summary>
        /// Set the DockingConfig SO to bake. Called from managed code during setup.
        /// </summary>
        public static void SetDockingConfig(DockingConfig config)
        {
            _pendingConfig = config;
        }

        protected override void OnUpdate()
        {
            if (_initialized)
            {
                Enabled = false;
                return;
            }

            if (_pendingConfig == null)
                return;

            _blobRef = DockingConfigBlob.BuildFromConfig(_pendingConfig);

            var entity = EntityManager.CreateEntity();
            EntityManager.AddComponentData(entity, new DockingConfigBlobComponent { Config = _blobRef });

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
