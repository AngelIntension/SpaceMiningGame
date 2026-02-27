# Contract: State Store Interface

Central immutable state store for player-domain state.

---

## IStateStore

```csharp
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
```

## Guarantees

1. **Thread safety**: `Current` is safe to read from any thread (immutable record). `Dispatch` MUST be called from main thread only.
2. **Atomicity**: Each `Dispatch` produces exactly one new state. No partial updates visible.
3. **Ordering**: Dispatches are processed sequentially in call order. No batching or deferred execution.
4. **Version monotonicity**: `Version` strictly increases by 1 per dispatch. Never resets.
5. **Notification (OnStateChanged)**: `OnStateChanged` fires exactly once per dispatch, after `Current` is updated. Fires even if the reducer returned the same state reference (identity unchanged).
6. **Notification (StateChangedEvent via EventBus)**: `StateChangedEvent<GameState>` is published via `IEventBus` only when the dispatch produces a **new state reference** (i.e., `!ReferenceEquals(oldState, newState)`). This is an optimization for view subscribers that only care about actual changes. Both notification mechanisms coexist — `OnStateChanged` for sync systems needing every dispatch, `StateChangedEvent` for views needing change-only updates.

## Reducer Pipeline

```
Dispatch(action)
  → GameStateReducer.Reduce(Current, action)
  → Current = newState
  → Version++
  → OnStateChanged fires
```

## ECS Integration

### Store → ECS (StoreToEcsSyncSystem)

```csharp
// Runs at start of SimulationSystemGroup
// Compares stored version against cached version
// Only copies changed state into ECS singleton components
if (stateStore.Version != _cachedVersion)
{
    _cachedVersion = stateStore.Version;
    // Push player intent into ECS components
}
```

### ECS → Store (EcsToStoreSyncSystem)

```csharp
// Runs at end of SimulationSystemGroup
// Drains NativeQueue action buffers
// Calls stateStore.Dispatch() for each buffered action
while (queue.TryDequeue(out var action))
{
    stateStore.Dispatch(MapToManagedAction(action));
}
```

## Lifecycle

- Registered as `Singleton` in VContainer `RootLifetimeScope`
- Initialized with default empty state on construction
- Persists across scene loads (DontDestroyOnLoad via LifetimeScope)
