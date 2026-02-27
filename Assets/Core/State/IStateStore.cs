using Cysharp.Threading.Tasks;

namespace VoidHarvest.Core.State
{
    /// <summary>
    /// Central immutable state store for player-domain state.
    /// All state changes go through Dispatch → Reducer → new state.
    /// See Constitution § I: Functional &amp; Immutable First.
    /// </summary>
    public interface IStateStore
    {
        /// <summary>
        /// Current immutable game state snapshot. Never null after initialization.
        /// </summary>
        GameState Current { get; }

        /// <summary>
        /// Monotonically increasing version counter. Incremented on every dispatch.
        /// ECS sync systems compare against cached version to skip unnecessary copies.
        /// </summary>
        int Version { get; }

        /// <summary>
        /// Dispatch an action through the reducer pipeline.
        /// Produces a new immutable state and increments version.
        /// Must be called on main thread only.
        /// </summary>
        void Dispatch(IGameAction action);

        /// <summary>
        /// Subscribe to state changes. Fires after every dispatch.
        /// </summary>
        IUniTaskAsyncEnumerable<GameState> OnStateChanged { get; }
    }
}
