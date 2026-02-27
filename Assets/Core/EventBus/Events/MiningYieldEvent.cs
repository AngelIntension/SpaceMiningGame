namespace VoidHarvest.Core.EventBus.Events
{
    /// <summary>
    /// Published when mining produces whole resource units.
    /// See MVP-06: Inventory tracks correct quantities.
    /// </summary>
    public readonly struct MiningYieldEvent
    {
        /// <summary>Ore type identifier for the yielded resource. See MVP-06.</summary>
        public readonly string OreId;
        /// <summary>Number of whole units yielded this tick. See MVP-06.</summary>
        public readonly int Quantity;

        /// <summary>
        /// Create a mining yield event. See MVP-06: Inventory tracks correct quantities.
        /// </summary>
        public MiningYieldEvent(string oreId, int quantity)
        {
            OreId = oreId;
            Quantity = quantity;
        }
    }
}
