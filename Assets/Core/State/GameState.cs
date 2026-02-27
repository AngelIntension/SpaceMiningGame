namespace VoidHarvest.Core.State
{
    /// <summary>
    /// Root immutable game state. All state changes go through reducers.
    /// See Constitution § I: Functional &amp; Immutable First.
    /// </summary>
    public sealed record GameState(
        GameLoopState Loop,
        ShipState ActiveShipPhysics,
        CameraState Camera,
        WorldState World
    );
}
