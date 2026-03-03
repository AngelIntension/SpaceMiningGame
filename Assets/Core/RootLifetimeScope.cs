using System.Collections.Immutable;
using UnityEngine;
using VContainer;
using VContainer.Unity;
using VoidHarvest.Core.EventBus;
using VoidHarvest.Core.State;
using VoidHarvest.Features.Ship.Systems;
using VoidHarvest.Features.Mining.Systems;
using VoidHarvest.Features.Docking.Data;
using VoidHarvest.Features.StationServices.Data;
using VoidHarvest.Features.StationServices.Systems;
using VoidHarvest.Features.World.Data;
using VoidHarvest.Features.Camera.Data;
using VoidHarvest.Features.Targeting.Data;
using VoidHarvest.Features.Targeting.Systems;
using CameraReducerReal = VoidHarvest.Features.Camera.Systems.CameraReducer;
using ShipStateReducerReal = VoidHarvest.Features.Ship.Systems.ShipStateReducer;
using MiningReducerReal = VoidHarvest.Features.Mining.Systems.MiningReducer;
using InventoryReducerReal = VoidHarvest.Features.Resources.Systems.InventoryReducer;
using DockingReducerReal = VoidHarvest.Features.Docking.Systems.DockingReducer;
using StationServicesReducerReal = VoidHarvest.Features.StationServices.Systems.StationServicesReducer;

/// <summary>
/// Root DI scope. Registers core singletons: EventBus, StateStore, GameStateReducer.
/// Lives on GameManager GameObject with DontDestroyOnLoad.
/// See Constitution § VI: Explicit Over Implicit.
/// </summary>
public sealed class RootLifetimeScope : LifetimeScope
{
    [SerializeField] private WorldDefinition worldDefinition;
    [SerializeField] private CameraConfig cameraConfig;

    /// <summary>
    /// Register core singletons: EventBus, StateStore with composite reducer. See MVP-12: Immutable state.
    /// </summary>
    protected override void Configure(IContainerBuilder builder)
    {
        // EventBus — singleton, cross-system communication
        builder.Register<UniTaskEventBus>(Lifetime.Singleton).As<IEventBus>();

        // WorldDefinition — singleton, data-driven world config (Spec 009)
        builder.RegisterInstance(worldDefinition);

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
        VoidHarvest.Features.Docking.Systems.DockingEventBridgeSystem.SetDependencies(stateStore, eventBus);

        Debug.Log("[VoidHarvest] RootLifetimeScope: ECS bridge systems wired.");
    }

    /// <summary>
    /// Composite reducer that routes to real feature reducers (not stubs).
    /// This method lives in Assembly-CSharp so it can reference all feature assemblies.
    /// Cross-cutting station services actions (transfer, repair) are handled here
    /// to atomically update multiple state slices.
    /// </summary>
    private static GameState CompositeReducer(GameState state, IGameAction action)
        => action switch
        {
            // Cross-cutting actions first (before single-slice routing)
            TransferToStationAction a => HandleTransferToStation(state, a),
            TransferToShipAction a    => HandleTransferToShip(state, a),
            RepairShipAction a        => HandleRepairShip(state, a),

            // Cross-cutting: dock/undock clears all target locks
            CompleteDockingAction a => HandleDockingWithLockClear(state, a),
            CompleteUndockingAction a => HandleUndockingWithLockClear(state, a),

            // Single-slice routing
            ICameraAction a    => state with { Camera = CameraReducerReal.Reduce(state.Camera, a) },
            IShipAction a      => state with { ActiveShipPhysics = ShipStateReducerReal.Reduce(state.ActiveShipPhysics, a) },
            IMiningAction a    => state with { Loop = state.Loop with { Mining = MiningReducerReal.Reduce(state.Loop.Mining, a) } },
            IInventoryAction a => state with { Loop = state.Loop with { Inventory = InventoryReducerReal.Reduce(state.Loop.Inventory, a) } },
            IDockingAction a   => state with { Loop = state.Loop with { Docking = DockingReducerReal.Reduce(state.Loop.Docking, a) } },
            IFleetAction a     => state with { Loop = state.Loop with { Fleet = FleetReducer.Reduce(state.Loop.Fleet, a) } },
            ITechAction a      => state with { Loop = state.Loop with { TechTree = TechTreeReducer.Reduce(state.Loop.TechTree, a) } },
            IMarketAction a    => state with { Loop = state.Loop with { Market = MarketReducer.Reduce(state.Loop.Market, a) } },
            IBaseAction a      => state with { Loop = state.Loop with { Base = BaseReducer.Reduce(state.Loop.Base, a) } },
            IStationServicesAction a => state with { Loop = state.Loop with { StationServices = StationServicesReducerReal.Reduce(state.Loop.StationServices, a) } },
            ITargetingAction a => state with { Loop = state.Loop with { Targeting = TargetingReducer.Reduce(state.Loop.Targeting, a) } },
            _ => state
        };

    /// <summary>
    /// Transfer resource from ship inventory to station storage.
    /// </summary>
    private static GameState HandleTransferToStation(GameState state, TransferToStationAction a)
    {
        if (a.Quantity <= 0) return state;

        var inventory = state.Loop.Inventory;
        if (!inventory.Stacks.TryGetValue(a.ResourceId, out var stack) || stack.Quantity < a.Quantity)
            return state;

        // Remove from ship
        var updatedInventory = InventoryReducerReal.Reduce(inventory, new RemoveResourceAction(a.ResourceId, a.Quantity));
        if (ReferenceEquals(updatedInventory, inventory))
            return state; // Removal failed

        // Add to station storage
        var updatedServices = StationServicesReducerReal.Reduce(
            state.Loop.StationServices,
            new AddToStationStorageAction(a.StationId, a.ResourceId, a.Quantity, a.VolumePerUnit));

        return state with
        {
            Loop = state.Loop with
            {
                Inventory = updatedInventory,
                StationServices = updatedServices
            }
        };
    }

    /// <summary>
    /// Transfer resource from station storage to ship inventory.
    /// </summary>
    private static GameState HandleTransferToShip(GameState state, TransferToShipAction a)
    {
        if (a.Quantity <= 0) return state;

        var services = state.Loop.StationServices;
        if (!services.StationStorages.TryGetValue(a.StationId, out var storage))
            return state;

        if (!storage.Stacks.TryGetValue(a.ResourceId, out var stack) || stack.Quantity < a.Quantity)
            return state;

        // Validate ship capacity
        var inventory = state.Loop.Inventory;
        var updatedInventory = InventoryReducerReal.Reduce(inventory, new AddResourceAction(a.ResourceId, a.Quantity, a.VolumePerUnit));
        if (ReferenceEquals(updatedInventory, inventory))
            return state; // Ship full

        // Remove from station storage
        var updatedServices = StationServicesReducerReal.Reduce(
            services,
            new RemoveFromStationStorageAction(a.StationId, a.ResourceId, a.Quantity));

        return state with
        {
            Loop = state.Loop with
            {
                Inventory = updatedInventory,
                StationServices = updatedServices
            }
        };
    }

    /// <summary>
    /// Repair ship: deduct credits and restore hull integrity.
    /// </summary>
    private static GameState HandleRepairShip(GameState state, RepairShipAction a)
    {
        if (a.Cost <= 0 && state.ActiveShipPhysics.HullIntegrity >= 1.0f)
            return state;

        if (state.Loop.StationServices.Credits < a.Cost)
            return state;

        // Deduct credits
        var updatedServices = state.Loop.StationServices with
        {
            Credits = state.Loop.StationServices.Credits - a.Cost
        };

        // Restore hull
        var updatedShip = state.ActiveShipPhysics with { HullIntegrity = a.NewIntegrity };

        return state with
        {
            Loop = state.Loop with { StationServices = updatedServices },
            ActiveShipPhysics = updatedShip
        };
    }

    /// <summary>
    /// Cross-cutting: complete docking AND clear all target locks.
    /// </summary>
    private static GameState HandleDockingWithLockClear(GameState state, CompleteDockingAction a)
    {
        var updatedDocking = DockingReducerReal.Reduce(state.Loop.Docking, a);
        var updatedTargeting = TargetingReducer.Reduce(state.Loop.Targeting, new ClearAllLocksAction());
        return state with
        {
            Loop = state.Loop with
            {
                Docking = updatedDocking,
                Targeting = updatedTargeting
            }
        };
    }

    /// <summary>
    /// Cross-cutting: complete undocking AND clear all target locks.
    /// </summary>
    private static GameState HandleUndockingWithLockClear(GameState state, CompleteUndockingAction a)
    {
        var updatedDocking = DockingReducerReal.Reduce(state.Loop.Docking, a);
        var updatedTargeting = TargetingReducer.Reduce(state.Loop.Targeting, new ClearAllLocksAction());
        return state with
        {
            Loop = state.Loop with
            {
                Docking = updatedDocking,
                Targeting = updatedTargeting
            }
        };
    }

    private GameState CreateDefaultGameState()
    {
        // Build station storages from WorldDefinition (Spec 009)
        var storagesBuilder = ImmutableDictionary<int, StationStorageState>.Empty;
        if (worldDefinition != null && worldDefinition.Stations != null)
        {
            foreach (var station in worldDefinition.Stations)
            {
                if (station != null)
                    storagesBuilder = storagesBuilder.Add(station.StationId, StationStorageState.Empty);
            }
        }

        var stationServices = StationServicesState.Empty with
        {
            StationStorages = storagesBuilder
        };

        // Build WorldState from WorldDefinition
        var worldStations = worldDefinition != null
            ? worldDefinition.BuildWorldStations()
            : ImmutableArray<StationData>.Empty;

        // Build CameraState from CameraConfig (Spec 009 US3)
        var camera = cameraConfig != null
            ? new CameraState(
                cameraConfig.DefaultYaw,
                cameraConfig.DefaultPitch,
                cameraConfig.DefaultDistance,
                false, 0f, 0f,
                cameraConfig.MinPitch,
                cameraConfig.MaxPitch,
                cameraConfig.MinDistance,
                cameraConfig.MaxDistance,
                cameraConfig.MinZoomDistance,
                cameraConfig.MaxZoomDistance)
            : CameraState.Default;

        // Build InventoryState from ship archetype (Spec 009 US4)
        var archetype = worldDefinition != null ? worldDefinition.StartingShipArchetype : null;
        var inventory = archetype != null
            ? new InventoryState(
                ImmutableDictionary<string, ResourceStack>.Empty,
                archetype.CargoSlots,
                archetype.CargoCapacity,
                0f)
            : InventoryState.Empty;

        return new GameState(
            Loop: new GameLoopState(
                ExploreState.Empty,
                MiningSessionState.Empty,
                inventory,
                stationServices,
                TechTreeState.Empty,
                FleetState.Empty,
                BaseState.Empty,
                MarketState.Empty,
                DockingState.Empty,
                TargetingState.Empty
            ),
            ActiveShipPhysics: ShipState.Default,
            Camera: camera,
            World: new WorldState(worldStations, 0f)
        );
    }
}
