using Unity.Mathematics;
using UnityEngine;

namespace VoidHarvest.Features.Docking.Data
{
    /// <summary>
    /// MonoBehaviour marker on station prefab GameObjects defining the docking port pose.
    /// InputBridge reads this when targeting a station and copies data into
    /// DockingStateComponent on the ship entity.
    /// NOT an ECS component — stations are regular GameObjects, not entities.
    /// </summary>
    public class DockingPortComponent : MonoBehaviour
    {
        [Tooltip("Local-space docking port position relative to the station.")]
        public float3 PortPosition;

        [Tooltip("Local-space orientation the ship should face when docked.")]
        public quaternion PortRotation = quaternion.identity;

        [Tooltip("Maximum range to initiate docking (meters).")]
        public float DockingRange = 500f;

        [Tooltip("Range where magnetic snap begins (meters).")]
        public float SnapRange = 30f;

        [Tooltip("Unique station identifier matching WorldState.StationData.Id.")]
        public int StationId;

        /// <summary>World-space docking port position.</summary>
        public float3 WorldPortPosition => (float3)transform.TransformPoint(PortPosition);

        /// <summary>World-space docking port rotation.</summary>
        public quaternion WorldPortRotation => math.mul((quaternion)transform.rotation, PortRotation);
    }
}
