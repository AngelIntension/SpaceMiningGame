namespace VoidHarvest.Core.State
{
    /// <summary>
    /// Composite state for all game loop subsystems.
    /// </summary>
    public sealed record GameLoopState(
        ExploreState Explore,
        MiningSessionState Mining,
        InventoryState Inventory,
        RefiningState Refining,
        TechTreeState TechTree,
        FleetState Fleet,
        BaseState Base,
        MarketState Market,
        DockingState Docking
    );
}
