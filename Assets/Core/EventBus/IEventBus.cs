using Cysharp.Threading.Tasks;

namespace VoidHarvest.Core.EventBus
{
    /// <summary>
    /// Cross-system event bus for decoupled communication.
    /// See Constitution V: Modularity &amp; Extensibility.
    /// </summary>
    public interface IEventBus
    {
        /// <summary>
        /// Publish an event to all subscribers. Synchronous, allocation-free for struct T.
        /// </summary>
        void Publish<T>(in T evt) where T : struct;

        /// <summary>
        /// Subscribe to events of type T. Returns an async enumerable that yields
        /// events as they are published. Caller must provide cancellation.
        /// </summary>
        IUniTaskAsyncEnumerable<T> Subscribe<T>() where T : struct;
    }
}
