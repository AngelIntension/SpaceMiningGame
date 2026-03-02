using UnityEngine;

namespace VoidHarvest.Features.StationServices.Data
{
    /// <summary>
    /// Maps station IDs to their service configurations.
    /// Registered in SceneLifetimeScope for view-layer lookups.
    /// Avoids circular dependency between Base and StationServices assemblies.
    /// See Spec 006: Station Services.
    /// </summary>
    [CreateAssetMenu(menuName = "VoidHarvest/Station Services Config Map")]
    public class StationServicesConfigMap : ScriptableObject
    {
        [System.Serializable]
        public struct StationServiceBinding
        {
            public int StationId;
            public StationServicesConfig Config;
        }

        [Tooltip("Per-station service configuration bindings.")]
        public StationServiceBinding[] Bindings;

        /// <summary>
        /// Look up the StationServicesConfig for a given station ID.
        /// Returns null if no binding found.
        /// </summary>
        public StationServicesConfig GetConfig(int stationId)
        {
            if (Bindings == null) return null;
            foreach (var b in Bindings)
            {
                if (b.StationId == stationId)
                    return b.Config;
            }
            return null;
        }
    }
}
