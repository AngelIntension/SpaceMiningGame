namespace VoidHarvest.Core.EventBus.Events
{
    /// <summary>Published when the ship begins a docking approach.</summary>
    public readonly struct DockingStartedEvent
    {
        public readonly int StationId;
        public DockingStartedEvent(int stationId) { StationId = stationId; }
    }

    /// <summary>Published when the ship successfully docks at a station.</summary>
    public readonly struct DockingCompletedEvent
    {
        public readonly int StationId;
        public DockingCompletedEvent(int stationId) { StationId = stationId; }
    }

    /// <summary>Published when the ship begins undocking from a station.</summary>
    public readonly struct UndockingStartedEvent
    {
        public readonly int StationId;
        public UndockingStartedEvent(int stationId) { StationId = stationId; }
    }

    /// <summary>Published when the ship completes undocking and returns to free flight.</summary>
    public readonly struct UndockCompletedEvent
    {
        public readonly int StationId;
        public UndockCompletedEvent(int stationId) { StationId = stationId; }
    }

    /// <summary>Published when a docking sequence is cancelled mid-approach.</summary>
    public readonly struct DockingCancelledEvent { }
}
