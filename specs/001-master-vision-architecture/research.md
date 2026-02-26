# Research: VoidHarvest Master Vision & Architecture

**Date**: 2026-02-26
**Spec**: `specs/001-master-vision-architecture/spec.md`

---

## R1. C# Language Version in Unity 6

**Decision**: Keep **C# 9.0** as the project language level.

**Rationale**: Unity 6 (6000.3.10f1) ships with `<LangVersion>9.0</LangVersion>` in auto-generated `.csproj` files. The Mono runtime with .NET Standard 2.1 API surface is the default. Verified directly from the project's `Assembly-CSharp.csproj`.

**Key implications**:
- `record` (reference type) — fully supported. Use for all domain state (InventoryState, ShipState, FleetState, etc.)
- `record struct` — **NOT available** (requires C# 10). Use `readonly struct` with manual equality for value-type performance-critical data.
- `with` expressions — supported on `record` types only. Not available on `readonly struct` in C# 9.
- Primary constructors on non-record types — **NOT available** (C# 12).
- `init` property accessors — supported.

**Practical pattern**:
- Domain state: `public sealed record InventoryState(...)` — heap-allocated but infrequent changes (acceptable for reducer pattern)
- DOTS components: `public readonly struct ShipComponent : IComponentData { ... }` — unmanaged, Burst-compatible
- Hot-path value types: `public readonly struct ThrustInput { ... }` with factory methods instead of `with`

**Alternatives rejected**:
| Alternative | Reason |
|---|---|
| Force C# 10 via `csc.rsp` | Unsupported; IDE mismatch; potential Mono runtime issues |
| Wait for Unity C# upgrade | No timeline; blocks progress |

**Action**: Resolve TODO(C#_VERSION) in constitution — `record struct` is not available. Update guidance to: `record` for domain data, `readonly struct` for DOTS/Burst value types.

---

## R2. DOTS Entities Package Compatibility

**Decision**: Target `com.unity.entities` **1.3.x** and `com.unity.entities.graphics` **1.3.x**.

**Rationale**: Entities 1.3.x is the release line designed for Unity 6000.x. It depends on `com.unity.collections` >= 2.4.x and `com.unity.burst` >= 1.8.x, both already present as transitive dependencies of URP 17.3.0.

**Already present (transitive from URP 17.3.0)**:
| Package | Version | Status |
|---|---|---|
| `com.unity.burst` | 1.8.28 | Already resolved |
| `com.unity.collections` | 2.6.2 | Already resolved |
| `com.unity.mathematics` | 1.3.3 | Already resolved |

**To install**:
| Package | Version | Source |
|---|---|---|
| `com.unity.entities` | 1.3.x (latest patch) | Unity Registry |
| `com.unity.entities.graphics` | 1.3.x (latest patch) | Unity Registry |

**Key Unity 6 DOTS considerations**:
1. **Baking workflow is mandatory** — no `ConvertToEntity`. Use `Baker<T>` classes in SubScenes.
2. **SubScene requirement** — ECS entities must originate from SubScenes or runtime `EntityManager.CreateEntity`.
3. **`IJobEntity`** replaces `IJobForEach`. `Entities.ForEach` is deprecated.
4. **`ISystem`** (unmanaged, Burst-compatible) preferred over `SystemBase` (managed) for zero-GC.
5. **`SystemAPI.Query<T>()`** is the preferred access pattern.
6. **Entities Graphics** requires shaders with `DOTS_INSTANCING_ON` keyword for custom shaders. URP Lit/Unlit already support this.

**Alternatives rejected**:
| Alternative | Reason |
|---|---|
| Skip DOTS entirely | Constitution mandates DOTS for simulation |
| Entities 1.2.x | Older; 1.3.x has bug fixes for Unity 6 |

---

## R3. VContainer (Dependency Injection)

**Decision**: Use **VContainer 1.16.x** via OpenUPM.

**Package**: `jp.hadashikick.vcontainer`

**Rationale**: Lightweight, source-generator-based (no reflection emit), IL2CPP-safe, explicitly registered (aligns with Constitution § VI — Explicit Over Implicit). Actively maintained.

**Configuration pattern** — Tiered LifetimeScope:
1. **RootLifetimeScope** (`DontDestroyOnLoad`) — Core infrastructure: EventBus, StateStore, reducers
2. **SceneLifetimeScope** (per-scene) — Scene-specific view bindings, camera, HUD

**DOTS integration**: VContainer does NOT manage DOTS systems (DOTS World owns them). Use bridge services registered in VContainer that communicate with ECS via singleton components or NativeQueue buffers.

**Alternatives rejected**:
| Alternative | Reason |
|---|---|
| Zenject | Heavier, reflection-based, maintenance concerns, IL2CPP stripping issues |
| Custom DI | Maintenance overhead for no benefit |

---

## R4. UniTask (Async + EventBus)

**Decision**: Use **UniTask 2.5.x** via OpenUPM.

**Package**: `com.cysharp.unitask`

**Rationale**: Zero-allocation async/await on Unity's PlayerLoop. `Channel<T>` provides lock-free, allocation-free message passing for struct events. `IUniTaskAsyncEnumerable<T>` enables reactive consumption patterns.

**EventBus pattern**: `Channel<T>` (unbounded single-consumer) as backbone:
- `Publish<T>(in T evt)` — synchronous `TryWrite` (allocation-free for structs)
- `Subscribe<T>()` — returns `IUniTaskAsyncEnumerable<T>` for async consumption
- Events are `readonly record struct` (immutable value types, no heap alloc)

**DOTS-to-MonoBehaviour bridge**: Thin bridge `SystemBase` drains DOTS `NativeQueue<T>` events into UniTask `Channel<T>` once per frame in `PresentationSystemGroup`.

**CONSTITUTION DEVIATION note**: The DOTS-to-managed bridge singleton for EventBus access from `SystemBase` requires a static reference. Document with `// CONSTITUTION DEVIATION: DOTS SystemBase cannot use constructor injection`.

**Alternatives rejected**:
| Alternative | Reason |
|---|---|
| Unity Awaitable (built-in) | Lacks `Channel<T>`, async enumerable, operator set |
| UniRx | GC-allocating; deprecated in favor of R3 |
| R3 (Reactive Extensions) | Additional dependency; UniTask Channel covers needs |

---

## R5. System.Collections.Immutable

**Decision**: Install via **NuGetForUnity** (`com.github-glitchenzo.nugetforunity`).

**Rationale**: NuGetForUnity handles framework targeting (selects `netstandard2.1` DLL), dependency resolution (`System.Memory`, `System.Runtime.CompilerServices.Unsafe`), and `.meta` file generation. IL2CPP-safe (no `Reflection.Emit`).

**Performance guidance**:
| Operation | `ImmutableArray<T>` | `ImmutableDictionary<K,V>` |
|---|---|---|
| Add single element | O(n) — copies array | O(log n) — tree |
| Lookup | O(1) — array index | O(log n) — tree |
| Iteration | O(n), cache-friendly | O(n), tree traversal |
| GC on mutation | New `T[]` allocated | New tree nodes |

**Usage boundary**:
- `ImmutableArray<T>` / `ImmutableDictionary<K,V>` — **player-domain state only** (inventory, fleet, tech tree). Changes at human-action frequency (< 10/sec).
- `NativeArray<T>` / `NativeHashMap<K,V>` — **DOTS simulation state** (asteroids, physics). Changes per-frame at 60 FPS.
- **NEVER copy between managed immutable collections and native containers.** If you find yourself doing this, the state boundary is drawn wrong.

**IL2CPP safeguard** — Add `Assets/link.xml`:
```xml
<linker>
  <assembly fullname="System.Collections.Immutable" preserve="all" />
  <assembly fullname="System.Memory" preserve="all" />
  <assembly fullname="System.Runtime.CompilerServices.Unsafe" preserve="all" />
</linker>
```

---

## R6. Cinemachine 3.x Camera System

**Decision**: Use `com.unity.cinemachine` **3.1.x** with composable component architecture.

**Key components**:
- `CinemachineCamera` (replaces `CinemachineVirtualCamera`)
- `CinemachineOrbitalFollow` (position/body — **Sphere** mode for space, no ground plane)
- `CinemachineRotationComposer` (rotation/aim)
- `CinemachineBrain` on Main Camera

**Namespace**: `Unity.Cinemachine` (not `Cinemachine` as in 2.x)

**Programmatic control**: Drive `HorizontalAxis.Value`, `VerticalAxis.Value`, and `Radius` directly from `CameraView` MonoBehaviour. **Disable or omit** `InputAxisController` — VoidHarvest's immutable pipeline (`CameraReducer → CameraState → CameraView`) replaces Cinemachine's input handling.

**Speed-based zoom**: `SpeedZoomAction` in reducer computes `TargetDistance`; `CameraView` smoothly interpolates `OrbitalFollow.Radius` via `Mathf.SmoothDamp`.

**URP**: No special setup required. `CinemachineBrain` on URP Camera works out of the box.

**Alternatives rejected**:
| Alternative | Reason |
|---|---|
| Cinemachine 2.x | Deprecated for Unity 6; would require migration |
| Custom camera (no Cinemachine) | Loses blending, noise, collision avoidance for free |

---

## R7. Immutable State + DOTS Hybrid Architecture

**Decision**: **Split canonical authority** — ECS owns high-frequency simulation state; central immutable store owns player-domain state.

### Architecture: "Thin Mutable Shell" Pattern

```
Central State Store (immutable records)        ECS World (mutable components)
  InventoryState, FleetState, TechTreeState       AsteroidComponent, ShipPhysicsComponent
  Updated via: Action → Reducer → new State       Updated via: Burst jobs, ISystem
  Read by: UI Views, Sync systems                 Read by: Entities Graphics, collision
```

### Frame Update Order ("Managed-Unmanaged-Managed Sandwich")

```
[1] Input System (Unity Input callbacks)
[2] PilotCommand construction (MonoBehaviour)
[3] ShipStateReducer (pure function) → central state store
[4] StoreToEcsSync (ISystem, OrderFirst) → pushes player intent into ECS
[5] ECS Simulation (SimulationSystemGroup) — Burst-compiled, pure ECS
[6] EcsToStoreSync (ISystem, OrderLast) → drains NativeQueue into store
[7] State Store notifications → UI views update
[8] Presentation (Entities Graphics renders, UI MonoBehaviours update)
```

### ECS-to-Reducer Communication

**NativeQueue action buffer** pattern:
- Burst jobs write unmanaged action structs into `NativeQueue<T>.ParallelWriter`
- Main-thread `ActionDispatchSystem` (OrderLast) drains queue into state store dispatches
- Zero managed allocation in hot loop; all managed work on main thread only

### Key Boundaries

| State | Canonical Owner | Sync Direction |
|---|---|---|
| Ship position/velocity | ECS (physics system) | ECS → Store (for HUD display) |
| Ship throttle/target | Store (from PilotCommand) | Store → ECS (for physics input) |
| Inventory | Store (reducer) | ECS → Store (mining yield actions) |
| Asteroid positions/ore | ECS (simulation) | Never synced to store |
| Tech tree | Store (reducer) | Never synced to ECS (UI only) |
| Fleet roster | Store (reducer) | Store → ECS (on ship swap only) |

### BlobAssets for Static Data

ScriptableObject data → BlobAssetReference<T> during baking/initialization. Provides:
- Genuine immutability after build
- Full Burst compatibility
- Zero-copy reference semantics
- Perfect for ore type definitions, ship archetype stats, tech tree structure

**Alternatives rejected**:
| Alternative | Reason |
|---|---|
| All state in ECS | Loses reducer pattern; no state diffing/notification for UI |
| All state in central store | Kills Burst/Jobs performance for simulation |
| Direct StateStore access from Burst | Impossible — Burst cannot access managed types |

---

## R8. Package Installation Plan (Consolidated)

### manifest.json additions

```json
{
  "scopedRegistries": [
    {
      "name": "OpenUPM",
      "url": "https://package.openupm.com",
      "scopes": [
        "jp.hadashikick",
        "com.cysharp",
        "com.github-glitchenzo"
      ]
    }
  ],
  "dependencies": {
    "com.unity.entities": "1.3.2",
    "com.unity.entities.graphics": "1.3.2",
    "com.unity.cinemachine": "3.1.2",
    "com.unity.addressables": "2.3.1",
    "jp.hadashikick.vcontainer": "1.16.7",
    "com.cysharp.unitask": "2.5.10",
    "com.github-glitchenzo.nugetforunity": "4.5.0"
  }
}
```

After Unity resolves packages, install `System.Collections.Immutable` via NuGetForUnity UI.

**Note**: Verify exact latest versions via Unity Package Manager / OpenUPM before committing. Version numbers above reflect May 2025 knowledge.

### Existing packages (no action needed)

| Package | Version | Source |
|---|---|---|
| `com.unity.burst` | 1.8.28 | Transitive via URP |
| `com.unity.collections` | 2.6.2 | Transitive via URP |
| `com.unity.mathematics` | 1.3.3 | Transitive via URP |
| `com.unity.inputsystem` | 1.18.0 | Already installed |
| `com.unity.test-framework` | 1.6.0 | Already installed |
