namespace VoidHarvest.Features.Mining.Data
{
    /// <summary>
    /// Static registry for looking up OreDefinition ScriptableObjects by OreId at runtime.
    /// Populated by OreTypeBlobBakingSystem during initialization.
    /// No DOTS dependency — safe to call from any assembly (StationServices, HUD, etc.).
    /// // CONSTITUTION DEVIATION: static registry for managed SO access from non-DOTS code
    /// </summary>
    public static class OreDefinitionRegistry
    {
        private static OreDefinition[] _definitions;

        /// <summary>
        /// Store the OreDefinition array for runtime lookup.
        /// Called once during OreTypeBlobBakingSystem.SetOreDefinitions().
        /// </summary>
        public static void SetDefinitions(OreDefinition[] definitions)
        {
            _definitions = definitions;
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
    }
}
