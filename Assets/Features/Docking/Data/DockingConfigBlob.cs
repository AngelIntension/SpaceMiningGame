using Unity.Collections;
using Unity.Entities;

namespace VoidHarvest.Features.Docking.Data
{
    /// <summary>
    /// Burst-accessible docking configuration baked from DockingConfig ScriptableObject.
    /// All docking tuning parameters live here for Burst-compiled DockingSystem access.
    /// See Spec 009: Data-Driven World Config (US2).
    /// </summary>
    public struct DockingConfigBlob
    {
        public float MaxDockingRange;
        public float SnapRange;
        public float SnapDuration;
        public float UndockClearanceDistance;
        public float UndockDuration;
        public float ApproachTimeout;
        public float AlignTimeout;
        public float AlignDotThreshold;
        public float AlignAngVelThreshold;

        /// <summary>
        /// Builds a BlobAssetReference from a DockingConfig ScriptableObject.
        /// Allocator.Persistent — caller must Dispose.
        /// </summary>
        public static BlobAssetReference<DockingConfigBlob> BuildFromConfig(DockingConfig config)
        {
            using var builder = new BlobBuilder(Allocator.Temp);
            ref var root = ref builder.ConstructRoot<DockingConfigBlob>();
            root.MaxDockingRange = config.MaxDockingRange;
            root.SnapRange = config.SnapRange;
            root.SnapDuration = config.SnapDuration;
            root.UndockClearanceDistance = config.UndockClearanceDistance;
            root.UndockDuration = config.UndockDuration;
            root.ApproachTimeout = config.ApproachTimeout;
            root.AlignTimeout = config.AlignTimeout;
            root.AlignDotThreshold = config.AlignDotThreshold;
            root.AlignAngVelThreshold = config.AlignAngVelThreshold;

            return builder.CreateBlobAssetReference<DockingConfigBlob>(Allocator.Persistent);
        }
    }

    /// <summary>
    /// Singleton ECS component holding the BlobAssetReference to DockingConfigBlob.
    /// Created by DockingConfigBlobBakingSystem.
    /// See Spec 009: Data-Driven World Config (US2).
    /// </summary>
    public struct DockingConfigBlobComponent : IComponentData
    {
        public BlobAssetReference<DockingConfigBlob> Config;
    }
}
