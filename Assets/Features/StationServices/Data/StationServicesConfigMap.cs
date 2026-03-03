using UnityEngine;

namespace VoidHarvest.Features.StationServices.Data
{
    /// <summary>
    /// Maps station IDs to their service configurations.
    /// Registered in SceneLifetimeScope for view-layer lookups.
    /// Avoids circular dependency between Base and StationServices assemblies.
    /// See Spec 006: Station Services.
    /// </summary>
    [CreateAssetMenu(menuName = "VoidHarvest/Station/Station Services Config Map")]
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

        private void OnValidate()
        {
            if (Bindings == null) return;
            var seen = new System.Collections.Generic.HashSet<int>();
            for (int i = 0; i < Bindings.Length; i++)
            {
                if (!seen.Add(Bindings[i].StationId))
                    Debug.LogWarning($"[{name}] Duplicate StationId {Bindings[i].StationId} at index {i}");
                if (Bindings[i].Config == null)
                    Debug.LogWarning($"[{name}] Bindings[{i}].Config must not be null");
            }
        }
    }
}
