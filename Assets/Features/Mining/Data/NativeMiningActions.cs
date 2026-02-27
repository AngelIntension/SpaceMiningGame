using Unity.Entities;

namespace VoidHarvest.Features.Mining.Data
{
    /// <summary>
    /// Unmanaged struct for mining yield data transported via NativeQueue.
    /// Written by MiningBeamSystem (Burst), drained by MiningActionDispatchSystem (managed).
    /// See MVP-05: Mining beam and yield.
    /// </summary>
    public struct NativeMiningYieldAction
    {
        /// <summary>Entity that produced the yield. See MVP-05.</summary>
        public Entity SourceAsteroid;
        /// <summary>Index into OreTypeBlobDatabase for the yielded ore. See MVP-05.</summary>
        public int OreTypeId;
        /// <summary>Raw yield amount this tick. See MVP-05.</summary>
        public float Amount;
    }

    /// <summary>
    /// Signals an asteroid has been fully depleted (RemainingMass <= 0).
    /// Written by MiningBeamSystem, drained by MiningActionDispatchSystem.
    /// </summary>
    public struct NativeAsteroidDepletedAction
    {
        /// <summary>Entity of the fully depleted asteroid. See MVP-05.</summary>
        public Entity Asteroid;
    }

    /// <summary>
    /// Signals mining stopped due to out-of-range or other reasons.
    /// Reason is int cast from StopReason enum for Burst compatibility.
    /// </summary>
    public struct NativeMiningStopAction
    {
        /// <summary>Entity of the asteroid that caused the stop. See MVP-05.</summary>
        public Entity SourceAsteroid;
        /// <summary>Stop reason as int (cast from StopReason enum for Burst). See MVP-05.</summary>
        public int Reason;
    }

    /// <summary>
    /// Tag-only singleton component. The NativeQueue is owned by MiningBeamSystem.
    /// See data-model.md NativeQueue Action Structs.
    /// </summary>
    public struct MiningActionBufferSingleton : IComponentData { }
}
