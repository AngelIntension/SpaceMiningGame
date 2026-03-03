using System.Collections.Generic;
using System.Collections.Immutable;
using Unity.Mathematics;
using UnityEngine;
using VoidHarvest.Core.State;
using VoidHarvest.Features.Ship.Data;
using VoidHarvest.Features.Station.Data;

namespace VoidHarvest.Features.World.Data
{
    /// <summary>
    /// Defines the complete station roster and player starting conditions for a game world.
    /// Designers author one asset per world configuration.
    /// See Spec 009: Data-Driven World Config.
    /// </summary>
    [CreateAssetMenu(menuName = "VoidHarvest/World/World Definition")]
    public class WorldDefinition : ScriptableObject
    {
        [Tooltip("All stations in this world. Each must have a unique StationId.")]
        public StationDefinition[] Stations;

        [Tooltip("Player starting world position.")]
        public Vector3 PlayerStartPosition;

        [Tooltip("Player starting world rotation.")]
        public Quaternion PlayerStartRotation = Quaternion.identity;

        [Tooltip("Ship archetype the player starts with.")]
        public ShipArchetypeConfig StartingShipArchetype;

        /// <summary>
        /// Build an ImmutableArray of StationData from the Stations array.
        /// Used by RootLifetimeScope to initialize WorldState.
        /// </summary>
        public ImmutableArray<StationData> BuildWorldStations()
        {
            if (Stations == null || Stations.Length == 0)
                return ImmutableArray<StationData>.Empty;

            var builder = ImmutableArray.CreateBuilder<StationData>(Stations.Length);
            for (int i = 0; i < Stations.Length; i++)
            {
                var s = Stations[i];
                if (s == null) continue;

                var services = s.AvailableServices != null
                    ? ImmutableArray.Create(s.AvailableServices)
                    : ImmutableArray<string>.Empty;

                builder.Add(new StationData(
                    s.StationId,
                    new float3(s.WorldPosition.x, s.WorldPosition.y, s.WorldPosition.z),
                    s.DisplayName ?? "",
                    services
                ));
            }
            return builder.ToImmutable();
        }

        /// <summary>
        /// Find a StationDefinition by station ID. Returns null if not found.
        /// </summary>
        public StationDefinition GetStationById(int stationId)
        {
            if (Stations == null) return null;
            for (int i = 0; i < Stations.Length; i++)
            {
                if (Stations[i] != null && Stations[i].StationId == stationId)
                    return Stations[i];
            }
            return null;
        }

        private void OnValidate()
        {
            if (Stations == null || Stations.Length == 0)
            {
                Debug.LogWarning($"[{name}] Stations must have at least one station");
            }
            else
            {
                var seen = new HashSet<int>();
                for (int i = 0; i < Stations.Length; i++)
                {
                    if (Stations[i] == null)
                    {
                        Debug.LogWarning($"[{name}] Stations[{i}] must not be null");
                        continue;
                    }
                    if (!seen.Add(Stations[i].StationId))
                        Debug.LogWarning($"[{name}] Duplicate StationId {Stations[i].StationId} at index {i}");
                }
            }

            if (StartingShipArchetype == null)
                Debug.LogWarning($"[{name}] StartingShipArchetype must not be null");
        }
    }
}
