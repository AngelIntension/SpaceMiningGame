namespace VoidHarvest.Core.State
{
    // NOTE: The root composite reducer now lives in RootLifetimeScope.CompositeReducer
    // (Assembly-CSharp) which uses the real feature reducers via type aliases.
    // This file only contains stub reducers for features not yet implemented.

    /// <summary>
    /// FleetReducer. Handles dock/undock station tracking. See spec 004.
    /// </summary>
    public static class FleetReducer
    {
        public static FleetState Reduce(FleetState state, IFleetAction action)
            => action switch
            {
                DockAtStationAction a => state with { DockedAtStation = Extensions.Option<int>.Some(a.StationId) },
                UndockFromStationAction => state with { DockedAtStation = default },
                _ => state
            };
    }

    /// <summary>
    /// Stub TechTreeReducer. Returns unchanged state. Phase 1+.
    /// </summary>
    public static class TechTreeReducer
    {
        /// <summary>
        /// Stub: returns unchanged state. Phase 1+.
        /// </summary>
        public static TechTreeState Reduce(TechTreeState state, ITechAction action) => state;
    }

    /// <summary>
    /// Stub MarketReducer. Returns unchanged state. Phase 3.
    /// </summary>
    public static class MarketReducer
    {
        /// <summary>
        /// Stub: returns unchanged state. Phase 3.
        /// </summary>
        public static MarketState Reduce(MarketState state, IMarketAction action) => state;
    }

    /// <summary>
    /// Stub BaseReducer. Returns unchanged state. Phase 2+.
    /// </summary>
    public static class BaseReducer
    {
        /// <summary>
        /// Stub: returns unchanged state. Phase 2+.
        /// </summary>
        public static BaseState Reduce(BaseState state, IBaseAction action) => state;
    }
}
