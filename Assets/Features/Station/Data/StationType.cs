namespace VoidHarvest.Features.Station.Data
{
    /// <summary>
    /// Classification of station types. Extensible — add new values as station types are designed.
    /// See Spec 009: Data-Driven World Config.
    /// </summary>
    public enum StationType
    {
        MiningRelay,
        RefineryHub,
        TradePost,
        ResearchStation
    }
}
