using System.Collections.Generic;

namespace VoidHarvest.Features.Mining.Data
{
    /// <summary>
    /// Static registry for looking up OreDefinition ScriptableObjects by OreId at runtime.
    /// Also provides a unified display name lookup for both ore IDs and material IDs.
    /// Populated by OreTypeBlobBakingSystem during initialization.
    /// No DOTS dependency — safe to call from any assembly (StationServices, HUD, etc.).
    /// // CONSTITUTION DEVIATION: static registry for managed SO access from non-DOTS code
    /// </summary>
    public static class OreDefinitionRegistry
    {
        private static OreDefinition[] _definitions;
        private static Dictionary<string, string> _displayNames;

        /// <summary>
        /// Store the OreDefinition array for runtime lookup.
        /// Builds a combined display name map covering both ore IDs and material IDs.
        /// Called once during OreTypeBlobBakingSystem.SetOreDefinitions().
        /// </summary>
        public static void SetDefinitions(OreDefinition[] definitions)
        {
            _definitions = definitions;
            _displayNames = new Dictionary<string, string>();

            if (definitions == null) return;
            foreach (var def in definitions)
            {
                if (def == null) continue;
                if (!string.IsNullOrEmpty(def.OreId))
                    _displayNames[def.OreId] = !string.IsNullOrEmpty(def.DisplayName) ? def.DisplayName : def.OreId;

                if (def.RefiningOutputs == null) continue;
                foreach (var output in def.RefiningOutputs)
                {
                    if (output.Material != null && !string.IsNullOrEmpty(output.Material.MaterialId))
                        _displayNames[output.Material.MaterialId] = !string.IsNullOrEmpty(output.Material.DisplayName)
                            ? output.Material.DisplayName
                            : output.Material.MaterialId;
                }
            }
        }

        /// <summary>
        /// Get the OreDefinition for a given OreId string. Returns null if not found.
        /// </summary>
        public static OreDefinition Get(string oreId)
        {
            if (_definitions == null || string.IsNullOrEmpty(oreId)) return null;
            foreach (var def in _definitions)
            {
                if (def != null && def.OreId == oreId)
                    return def;
            }
            return null;
        }

        /// <summary>
        /// Get the display name for any resource ID (ore or material).
        /// Falls back to the raw ID if not found.
        /// </summary>
        public static string GetDisplayName(string resourceId)
        {
            if (string.IsNullOrEmpty(resourceId)) return resourceId ?? "";
            if (_displayNames != null && _displayNames.TryGetValue(resourceId, out var name))
                return name;
            return resourceId;
        }
    }
}
