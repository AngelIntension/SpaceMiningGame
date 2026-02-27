using System.Collections.Immutable;
using UnityEngine;
using VContainer;
using VContainer.Unity;
using VoidHarvest.Core.EventBus;
using VoidHarvest.Core.State;
using VoidHarvest.Features.Ship.Systems;
using VoidHarvest.Features.Mining.Systems;
using CameraReducerReal = VoidHarvest.Features.Camera.Systems.CameraReducer;
using ShipStateReducerReal = VoidHarvest.Features.Ship.Systems.ShipStateReducer;
using MiningReducerReal = VoidHarvest.Features.Mining.Systems.MiningReducer;
using InventoryReducerReal = VoidHarvest.Features.Resources.Systems.InventoryReducer;

/// <summary>
/// Root DI scope. Registers core singletons: EventBus, StateStore, GameStateReducer.
/// Lives on GameManager GameObject with DontDestroyOnLoad.
/// See Constitution § VI: Explicit Over Implicit.
/// </summary>
public sealed class RootLifetimeScope : LifetimeScope
{
    /// <summary>
    /// Register core singletons: EventBus, StateStore with composite reducer. See MVP-12: Immutable state.
    /// </summary>
    protected override void Configure(IContainerBuilder builder)
    {
        // EventBus — singleton, cross-system communication
        builder.Register<UniTaskEventBus>(Lifetime.Singleton).As<IEventBus>();

        // Build the composite reducer that delegates to real feature reducers
        var initialState = CreateDefaultGameState();

        builder.Register<StateStore>(Lifetime.Singleton)
            .WithParameter<System.Func<GameState, IGameAction, GameState>>(CompositeReducer)
            .WithParameter(initialState)
            .As<IStateStore>();
    }

    private void Start()
    {
        // Wire ECS bridge systems after DI container is built
        var stateStore = Container.Resolve<IStateStore>();
        var eventBus = Container.Resolve<IEventBus>();

        EcsToStoreSyncSystem.SetStateStore(stateStore);
        MiningActionDispatchSystem.SetDependencies(stateStore, eventBus);

        Debug.Log("[VoidHarvest] RootLifetimeScope: ECS bridge systems wired.");
    }

    /// <summary>
    /// Composite reducer that routes to real feature reducers (not stubs).
    /// This method lives in Assembly-CSharp so it can reference all feature assemblies.
    /// </summary>
    private static GameState CompositeReducer(GameState state, IGameAction action)
        => action switch
        {
            ICameraAction a    => state with { Camera = CameraReducerReal.Reduce(state.Camera, a) },
            IShipAction a      => state with { ActiveShipPhysics = ShipStateReducerReal.Reduce(state.ActiveShipPhysics, a) },
            IMiningAction a    => state with { Loop = state.Loop with { Mining = MiningReducerReal.Reduce(state.Loop.Mining, a) } },
            IInventoryAction a => state with { Loop = state.Loop with { Inventory = InventoryReducerReal.Reduce(state.Loop.Inventory, a) } },
            IFleetAction a     => state with { Loop = state.Loop with { Fleet = FleetReducer.Reduce(state.Loop.Fleet, a) } },
            ITechAction a      => state with { Loop = state.Loop with { TechTree = TechTreeReducer.Reduce(state.Loop.TechTree, a) } },
            IMarketAction a    => state with { Loop = state.Loop with { Market = MarketReducer.Reduce(state.Loop.Market, a) } },
            IBaseAction a      => state with { Loop = state.Loop with { Base = BaseReducer.Reduce(state.Loop.Base, a) } },
            _ => state
        };

    private static GameState CreateDefaultGameState()
    {
        return new GameState(
            Loop: new GameLoopState(
                ExploreState.Empty,
                MiningSessionState.Empty,
                InventoryState.Empty,
                RefiningState.Empty,
                TechTreeState.Empty,
                FleetState.Empty,
                BaseState.Empty,
                MarketState.Empty
            ),
            ActiveShipPhysics: ShipState.Default,
            Camera: CameraState.Default,
            World: new WorldState(
                ImmutableArray<StationData>.Empty,
                0f
            )
        );
    }
}
