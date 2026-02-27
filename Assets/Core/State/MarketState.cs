using System.Collections.Immutable;

namespace VoidHarvest.Core.State
{
    /// <summary>
    /// Market state. Stub — Phase 3.
    /// </summary>
    public sealed record MarketState(
        ImmutableDictionary<string, CommodityMarket> Commodities,
        float GlobalDemandMultiplier,
        int TickCount
    )
    {
        public static readonly MarketState Empty = new(
            ImmutableDictionary<string, CommodityMarket>.Empty, 1.0f, 0
        );
    }

    /// <summary>
    /// Market data for a single commodity including price, supply, demand and orders. Phase 3.
    /// </summary>
    public sealed record CommodityMarket(
        string CommodityId,
        float BasePrice,
        float CurrentPrice,
        float Supply,
        float Demand,
        float PriceElasticity,
        ImmutableArray<MarketOrder> OpenOrders
    );

    /// <summary>
    /// A buy or sell order on the market. Phase 3.
    /// </summary>
    public sealed record MarketOrder(
        string OrderId,
        string CommodityId,
        OrderSide Side,
        int Quantity,
        float PriceLimit,
        string IssuerId
    );

    /// <summary>
    /// Side of a market order: Buy or Sell. Phase 3.
    /// </summary>
    public enum OrderSide { Buy, Sell }
}
