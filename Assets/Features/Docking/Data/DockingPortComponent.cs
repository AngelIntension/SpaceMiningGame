using Unity.Mathematics;
using UnityEngine;
using VoidHarvest.Features.Station.Data;

namespace VoidHarvest.Features.Docking.Data
{
    /// <summary>
    /// MonoBehaviour marker on station prefab GameObjects defining the docking port pose.
    /// InputBridge reads this when targeting a station and copies data into
    /// DockingStateComponent on the ship entity.
    /// NOT an ECS component — stations are regular GameObjects, not entities.
    /// See Spec 009: Data-Driven World Config (FR-025).
    /// </summary>
    public class DockingPortComponent : MonoBehaviour
    {
        [Tooltip("Local-space docking port position relative to the station.")]
        public float3 PortPosition;

        [Tooltip("Local-space orientation the ship should face when docked.")]
        public quaternion PortRotation = quaternion.identity;

        [Tooltip("Station definition — StationId derived from this SO in Awake().")]
        [SerializeField] private StationDefinition stationDefinition;

        /// <summary>Station ID derived from StationDefinition at runtime.</summary>
        public int StationId { get; private set; }

        /// <summary>World-space docking port position.</summary>
        public float3 WorldPortPosition => (float3)transform.TransformPoint(PortPosition);

        /// <summary>World-space docking port rotation.</summary>
        public quaternion WorldPortRotation => math.mul((quaternion)transform.rotation, PortRotation);

        private void Awake()
        {
            if (stationDefinition != null)
                StationId = stationDefinition.StationId;
        }
    }
}
