using System;
using Cysharp.Threading.Tasks;
using VoidHarvest.Core.EventBus;
using VoidHarvest.Core.EventBus.Events;

namespace VoidHarvest.Core.State
{
    /// <summary>
    /// Concrete state store implementation using GameStateReducer.
    /// Dispatch is main-thread only. Current is safe to read from any thread.
    /// See Constitution § I: Functional &amp; Immutable First.
    /// </summary>
    public sealed class StateStore : IStateStore, IDisposable
    {
        private readonly Func<GameState, IGameAction, GameState> _reducer;
        private readonly IEventBus _eventBus;
        private readonly Channel<GameState> _stateChannel;

        private GameState _current;
        private int _version;

        /// <summary>
        /// Current immutable game state snapshot. See MVP-12: Immutable state.
        /// </summary>
        public GameState Current => _current;

        /// <summary>
        /// Monotonically increasing version counter, incremented on every dispatch. See MVP-12: Immutable state.
        /// </summary>
        public int Version => _version;

        /// <summary>
        /// Async enumerable of state snapshots emitted after every dispatch. See MVP-12: Immutable state.
        /// </summary>
        public IUniTaskAsyncEnumerable<GameState> OnStateChanged =>
            _stateChannel.Reader.ReadAllAsync();

        /// <summary>
        /// Create a new state store with the given reducer and initial state.
        /// </summary>
        /// <param name="reducer">Pure function: (state, action) → state</param>
        /// <param name="initialState">Initial game state</param>
        /// <param name="eventBus">Event bus for publishing StateChangedEvent</param>
        public StateStore(
            Func<GameState, IGameAction, GameState> reducer,
            GameState initialState,
            IEventBus eventBus)
        {
            _reducer = reducer ?? throw new ArgumentNullException(nameof(reducer));
            _current = initialState ?? throw new ArgumentNullException(nameof(initialState));
            _eventBus = eventBus ?? throw new ArgumentNullException(nameof(eventBus));
            _stateChannel = Channel.CreateSingleConsumerUnbounded<GameState>();
        }

        /// <summary>
        /// Dispatch an action through the reducer, producing a new immutable state. See MVP-12: Immutable state.
        /// </summary>
        public void Dispatch(IGameAction action)
        {
            if (action == null) return;

            var oldState = _current;
            var newState = _reducer(oldState, action);

            _current = newState;
            _version++;

            // OnStateChanged fires on every dispatch (even if state unchanged)
            _stateChannel.Writer.TryWrite(newState);

            // StateChangedEvent only fires when state reference actually changed
            if (!ReferenceEquals(oldState, newState))
            {
                _eventBus.Publish(new StateChangedEvent<GameState>(oldState, newState));
            }
        }

        /// <summary>
        /// Completes the state channel, releasing subscribers. See MVP-12: Immutable state.
        /// </summary>
        public void Dispose()
        {
            _stateChannel.Writer.TryComplete();
        }
    }
}
