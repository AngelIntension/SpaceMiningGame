namespace VoidHarvest.Features.Mining.Data
{
    /// <summary>
    /// Static registry mapping OreId strings and OreTypeId indices to human-readable display names.
    /// Populated by OreTypeBlobBakingSystem during initialization.
    /// No DOTS dependency — safe to call from any assembly (HUD, Input, etc.).
    /// </summary>
    public static class OreDisplayNames
    {
        private static string[] _byIndex;
        private static System.Collections.Generic.Dictionary<string, string> _byOreId;

        /// <summary>
        /// Populate the lookup tables from OreDefinition data.
        /// Called once during OreTypeBlobBakingSystem.SetOreDefinitions().
        /// </summary>
        public static void SetLookups(string[] displayNamesByIndex,
            System.Collections.Generic.Dictionary<string, string> oreIdToDisplayName)
        {
            _byIndex = displayNamesByIndex;
            _byOreId = oreIdToDisplayName;
        }

        /// <summary>
        /// Get the display name for an OreTypeId index. Returns empty string if out of range.
        /// </summary>
        public static string Get(int oreTypeId)
        {
            if (_byIndex == null || oreTypeId < 0 || oreTypeId >= _byIndex.Length)
                return "";
            return _byIndex[oreTypeId];
        }

        /// <summary>
        /// Get the display name for an OreId string. Falls back to the OreId if not found.
        /// </summary>
        public static string Get(string oreId)
        {
            if (_byOreId != null && _byOreId.TryGetValue(oreId, out var name))
                return name;
            return oreId;
        }
    }
}
