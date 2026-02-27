using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;

namespace VoidHarvest.Core.EventBus
{
    /// <summary>
    /// UniTask Channel-backed event bus. Zero-allocation publish for struct events.
    /// Supports multiple subscribers per event type via per-subscriber channels.
    /// See Constitution V: Modularity &amp; Extensibility.
    /// </summary>
    public sealed class UniTaskEventBus : IEventBus, IDisposable
    {
        private readonly ConcurrentDictionary<Type, object> _broadcasters = new();

        /// <summary>
        /// Publish a struct event to all active subscribers. See MVP-12: Immutable state.
        /// </summary>
        public void Publish<T>(in T evt) where T : struct
        {
            if (_broadcasters.TryGetValue(typeof(T), out var broadcaster))
            {
                ((Broadcaster<T>)broadcaster).Publish(evt);
            }
        }

        /// <summary>
        /// Subscribe to events of type T via a per-subscriber async channel. See MVP-12: Immutable state.
        /// </summary>
        public IUniTaskAsyncEnumerable<T> Subscribe<T>() where T : struct
        {
            var broadcaster = (Broadcaster<T>)_broadcasters.GetOrAdd(
                typeof(T), _ => new Broadcaster<T>());
            return broadcaster.Subscribe();
        }

        /// <summary>
        /// Dispose all broadcaster channels, completing all active subscriptions. See MVP-12: Immutable state.
        /// </summary>
        public void Dispose()
        {
            foreach (var kvp in _broadcasters)
            {
                if (kvp.Value is IDisposable disposable)
                    disposable.Dispose();
            }
            _broadcasters.Clear();
        }

        private sealed class Broadcaster<T> : IDisposable where T : struct
        {
            private readonly object _lock = new();
            private readonly List<ChannelWriter<T>> _writers = new();
            private bool _disposed;

            public void Publish(in T evt)
            {
                lock (_lock)
                {
                    for (int i = _writers.Count - 1; i >= 0; i--)
                    {
                        if (!_writers[i].TryWrite(evt))
                        {
                            _writers.RemoveAt(i);
                        }
                    }
                }
            }

            public IUniTaskAsyncEnumerable<T> Subscribe()
            {
                var channel = Channel.CreateSingleConsumerUnbounded<T>();
                lock (_lock)
                {
                    if (_disposed)
                        throw new ObjectDisposedException(nameof(Broadcaster<T>));
                    _writers.Add(channel.Writer);
                }
                return channel.Reader.ReadAllAsync();
            }

            public void Dispose()
            {
                lock (_lock)
                {
                    _disposed = true;
                    foreach (var writer in _writers)
                    {
                        writer.TryComplete();
                    }
                    _writers.Clear();
                }
            }
        }
    }
}
