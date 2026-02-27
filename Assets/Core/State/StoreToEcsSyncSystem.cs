using Unity.Burst;
using Unity.Entities;

namespace VoidHarvest.Core.State
{
    /// <summary>
    /// Store-to-ECS sync. Empty placeholder in MVP.
    /// Phase 1+ will inject store→ECS sync logic for fleet ship swap.
    /// See research.md § R7.
    /// </summary>
    [UpdateInGroup(typeof(SimulationSystemGroup), OrderFirst = true)]
    public partial struct StoreToEcsSyncSystem : ISystem
    {
        /// <summary>
        /// Initialize sync system. No-op in MVP.
        /// </summary>
        public void OnCreate(ref SystemState state) { }

        /// <summary>
        /// Push store state into ECS. No-op in MVP; Phase 1+ will sync fleet ship swap.
        /// </summary>
        public void OnUpdate(ref SystemState state)
        {
            // No-op in MVP. Phase 1+ will push store state into ECS here.
        }

        /// <summary>
        /// Clean up sync system resources. No-op in MVP.
        /// </summary>
        public void OnDestroy(ref SystemState state) { }
    }
}
