namespace VoidHarvest.Features.StationServices.Data
{
    /// <summary>
    /// Events published by station services view layer and RefiningJobTicker.
    /// All zero-allocation readonly structs.
    /// See Spec 006: Station Services.
    /// </summary>

    public readonly struct RefiningJobStartedEvent
    {
        public readonly int StationId;
        public readonly string JobId;

        public RefiningJobStartedEvent(int stationId, string jobId)
        {
            StationId = stationId;
            JobId = jobId;
        }
    }

    public readonly struct RefiningJobCompletedEvent
    {
        public readonly int StationId;
        public readonly string JobId;

        public RefiningJobCompletedEvent(int stationId, string jobId)
        {
            StationId = stationId;
            JobId = jobId;
        }
    }

    public readonly struct ResourcesSoldEvent
    {
        public readonly string ResourceId;
        public readonly int Quantity;
        public readonly int TotalCredits;

        public ResourcesSoldEvent(string resourceId, int quantity, int totalCredits)
        {
            ResourceId = resourceId;
            Quantity = quantity;
            TotalCredits = totalCredits;
        }
    }

    public readonly struct CargoTransferredEvent
    {
        public readonly string ResourceId;
        public readonly int Quantity;
        public readonly bool ToStation;

        public CargoTransferredEvent(string resourceId, int quantity, bool toStation)
        {
            ResourceId = resourceId;
            Quantity = quantity;
            ToStation = toStation;
        }
    }

    public readonly struct ShipRepairedEvent
    {
        public readonly int Cost;
        public readonly float NewIntegrity;

        public ShipRepairedEvent(int cost, float newIntegrity)
        {
            Cost = cost;
            NewIntegrity = newIntegrity;
        }
    }

    public readonly struct CreditsChangedEvent
    {
        public readonly int OldBalance;
        public readonly int NewBalance;

        public CreditsChangedEvent(int oldBalance, int newBalance)
        {
            OldBalance = oldBalance;
            NewBalance = newBalance;
        }
    }
}
