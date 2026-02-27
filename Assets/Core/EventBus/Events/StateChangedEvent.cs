namespace VoidHarvest.Core.EventBus.Events
{
    /// <summary>
    /// Published by StateStore when a dispatch produces a new state reference.
    /// Views can subscribe to this as an alternative to StateStore.OnStateChanged.
    /// See Constitution § I: Functional &amp; Immutable First.
    /// </summary>
    public readonly struct StateChangedEvent<T> where T : class
    {
        /// <summary>State snapshot before the dispatch. See MVP-12: Immutable state.</summary>
        public readonly T PreviousState;
        /// <summary>State snapshot after the dispatch. See MVP-12: Immutable state.</summary>
        public readonly T CurrentState;

        /// <summary>
        /// Create a state changed event with before and after snapshots. See MVP-12: Immutable state.
        /// </summary>
        public StateChangedEvent(T previous, T current)
        {
            PreviousState = previous;
            CurrentState = current;
        }
    }
}
