using Unity.Entities;
using Unity.Mathematics;
using VoidHarvest.Core.State;

namespace VoidHarvest.Features.Docking.Data
{
    // CONSTITUTION DEVIATION: ECS mutable shell — IComponentData structs use mutable fields
    // as required by Unity ECS. Immutability is enforced at the reducer/state layer.

    /// <summary>
    /// ECS component added to the ship entity during a docking sequence.
    /// Removed when undocking completes.
    /// </summary>
    public struct DockingStateComponent : IComponentData
    {
        public DockingPhase Phase;
        public float3 TargetPortPosition;
        public quaternion TargetPortRotation;
        public int TargetStationId;
        public float SnapTimer;
        public float3 StartPosition;
        public quaternion StartRotation;
    }

    /// <summary>
    /// Singleton ECS component for Burst-to-managed event bridging.
    /// Written by DockingSystem (Burst), read and cleared by DockingEventBridgeSystem (managed).
    /// Preserves zero-GC guarantee in the Burst hot path.
    /// </summary>
    public struct DockingEventFlags : IComponentData
    {
        public bool DockCompleted;
        public int DockStationId;
        public bool UndockCompleted;
    }
}
