# Contract: EventBus Interface

Cross-system communication between DOTS, MonoBehaviour, and UI layers.

---

## IEventBus

```csharp
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
```

## Guarantees

1. **Zero-allocation** for `Publish` when `T` is a `struct` (uses `Channel<T>.TryWrite`)
2. **Main-thread delivery** for subscribers consuming via `await foreach` on Unity PlayerLoop
3. **No ordering guarantee** between different event types
4. **Same-type ordering** preserved (FIFO within a single `Channel<T>`)
5. **Fire-and-forget** — publisher does not wait for subscribers

## Event Type Contract

All events MUST be:
- `readonly struct` — no heap allocation, no boxing
- Immutable — all fields `readonly`
- Self-contained — no references to mutable objects or ECS entities
- Named with `Event` suffix (e.g., `MiningYieldEvent`)

## DOTS-to-Managed Bridge Contract

Since DOTS `ISystem` / `IJobEntity` cannot call managed `IEventBus.Publish`:

1. DOTS systems write to `NativeQueue<T>.ParallelWriter` (Burst-safe)
2. A managed `SystemBase` in `PresentationSystemGroup` drains the queue
3. The bridge system calls `IEventBus.Publish` for each dequeued action

```
[Burst Job] → NativeQueue<T> → [Bridge SystemBase] → IEventBus.Publish<T>
```

Latency: events from DOTS arrive at subscribers the same frame, after simulation completes.

## Lifecycle

- `IEventBus` registered as `Singleton` in VContainer `RootLifetimeScope`
- Disposed when `RootLifetimeScope` is destroyed
- Subscribers must pass `CancellationToken` (typically `destroyCancellationToken` from MonoBehaviour)
