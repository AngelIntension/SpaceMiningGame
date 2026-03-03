using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.UIElements;
using VContainer;
using VoidHarvest.Core.EventBus;
using VoidHarvest.Core.EventBus.Events;
using VoidHarvest.Core.State;
using VoidHarvest.Features.Mining.Data;
using VoidHarvest.Features.StationServices.Data;

namespace VoidHarvest.Features.StationServices.Views
{
    /// <summary>
    /// Controller for bidirectional cargo transfer between ship and station.
    /// See Spec 006 US1: Cargo Transfer.
    /// </summary>
    public sealed class CargoTransferPanelController : MonoBehaviour
    {
        private IStateStore _stateStore;
        private IEventBus _eventBus;
        private int _dockedStationId;

        private VisualElement _root;
        private ScrollView _shipItems;
        private ScrollView _stationItems;
        private SliderInt _quantitySlider;
        private Label _shipVolume;
        private Label _errorLabel;
        private Button _btnToStation;
        private Button _btnToShip;

        private string _selectedShipResource;
        private string _selectedStationResource;
        private CancellationTokenSource _stateCts;
        private InventoryState _lastInventory;
        private StationServicesState _lastServices;

        [Inject]
        public void Construct(IStateStore stateStore, IEventBus eventBus)
        {
            _stateStore = stateStore;
            _eventBus = eventBus;
        }

        public void Initialize(VisualElement root, int stationId)
        {
            _root = root;
            _dockedStationId = stationId;

            _shipItems = _root.Q<ScrollView>("ship-items");
            _stationItems = _root.Q<ScrollView>("station-items");
            _quantitySlider = _root.Q<SliderInt>("quantity-slider");
            _shipVolume = _root.Q<Label>("ship-volume");
            _errorLabel = _root.Q<Label>("cargo-error");
            _btnToStation = _root.Q<Button>("btn-to-station");
            _btnToShip = _root.Q<Button>("btn-to-ship");

            _btnToStation?.RegisterCallback<ClickEvent>(_ => OnTransferToStation());
            _btnToShip?.RegisterCallback<ClickEvent>(_ => OnTransferToShip());

            _stateCts?.Cancel();
            _stateCts = new CancellationTokenSource();
            ListenForStateChanges(_stateCts.Token).Forget();

            RefreshUI();
        }

        public void Cleanup()
        {
            _stateCts?.Cancel();
            _stateCts?.Dispose();
            _stateCts = null;
        }

        private void OnDestroy()
        {
            Cleanup();
        }

        private async UniTaskVoid ListenForStateChanges(CancellationToken ct)
        {
            await foreach (var evt in _eventBus.Subscribe<StateChangedEvent<GameState>>().WithCancellation(ct))
            {
                var inv = evt.CurrentState.Loop.Inventory;
                var svc = evt.CurrentState.Loop.StationServices;
                if (ReferenceEquals(inv, _lastInventory) && ReferenceEquals(svc, _lastServices))
                    continue;
                _lastInventory = inv;
                _lastServices = svc;
                RefreshUI();
            }
        }

        private void RefreshUI()
        {
            if (_stateStore == null || _root == null) return;

            var state = _stateStore.Current;
            var inventory = state.Loop.Inventory;

            // Ship cargo list
            _shipItems?.Clear();
            foreach (var kvp in inventory.Stacks)
            {
                var btn = new Button { text = $"{OreDefinitionRegistry.GetDisplayName(kvp.Key)}: {kvp.Value.Quantity}" };
                btn.AddToClassList("item-row");
                var resId = kvp.Key;
                if (resId == _selectedShipResource) btn.AddToClassList("item-row--selected");
                btn.RegisterCallback<ClickEvent>(_ => SelectShipResource(resId));
                _shipItems?.Add(btn);
            }

            if (_shipVolume != null)
                _shipVolume.text = $"Volume: {inventory.CurrentVolume:F1} / {inventory.MaxVolume:F0}";

            // Station storage list
            _stationItems?.Clear();
            if (state.Loop.StationServices.StationStorages.TryGetValue(_dockedStationId, out var storage))
            {
                foreach (var kvp in storage.Stacks)
                {
                    var btn = new Button { text = $"{OreDefinitionRegistry.GetDisplayName(kvp.Key)}: {kvp.Value.Quantity}" };
                    btn.AddToClassList("item-row");
                    var resId = kvp.Key;
                    if (resId == _selectedStationResource) btn.AddToClassList("item-row--selected");
                    btn.RegisterCallback<ClickEvent>(_ => SelectStationResource(resId));
                    _stationItems?.Add(btn);
                }
            }

            if (_errorLabel != null) _errorLabel.text = "";
        }

        private void SelectShipResource(string resId)
        {
            _selectedShipResource = resId;
            _selectedStationResource = null;
            ApplySelectionClasses();
            ResetSlider();
        }

        private void SelectStationResource(string resId)
        {
            _selectedStationResource = resId;
            _selectedShipResource = null;
            ApplySelectionClasses();
            ResetSlider();
        }

        private void ApplySelectionClasses()
        {
            if (_shipItems != null)
            {
                foreach (var child in _shipItems.Children())
                {
                    if (child is Button btn)
                    {
                        var resId = btn.text.Split(':')[0];
                        if (resId == _selectedShipResource)
                            btn.AddToClassList("item-row--selected");
                        else
                            btn.RemoveFromClassList("item-row--selected");
                    }
                }
            }

            if (_stationItems != null)
            {
                foreach (var child in _stationItems.Children())
                {
                    if (child is Button btn)
                    {
                        var resId = btn.text.Split(':')[0];
                        if (resId == _selectedStationResource)
                            btn.AddToClassList("item-row--selected");
                        else
                            btn.RemoveFromClassList("item-row--selected");
                    }
                }
            }
        }

        private void ResetSlider()
        {
            if (_quantitySlider == null) return;
            var state = _stateStore.Current;

            if (_selectedShipResource != null && state.Loop.Inventory.Stacks.TryGetValue(_selectedShipResource, out var shipStack))
            {
                _quantitySlider.highValue = Mathf.Max(1, shipStack.Quantity);
                _quantitySlider.value = 1;
            }
            else if (_selectedStationResource != null
                && state.Loop.StationServices.StationStorages.TryGetValue(_dockedStationId, out var storage)
                && storage.Stacks.TryGetValue(_selectedStationResource, out var stationStack))
            {
                _quantitySlider.highValue = Mathf.Max(1, stationStack.Quantity);
                _quantitySlider.value = 1;
            }
        }

        private void OnTransferToStation()
        {
            if (_selectedShipResource == null) return;
            int qty = _quantitySlider?.value ?? 1;

            var inventory = _stateStore.Current.Loop.Inventory;
            if (!inventory.Stacks.TryGetValue(_selectedShipResource, out var stack)) return;

            _stateStore.Dispatch(new TransferToStationAction(_dockedStationId, _selectedShipResource, qty, stack.VolumePerUnit));
            _eventBus?.Publish(new CargoTransferredEvent(_selectedShipResource, qty, true));
        }

        private void OnTransferToShip()
        {
            if (_selectedStationResource == null) return;
            int qty = _quantitySlider?.value ?? 1;

            var services = _stateStore.Current.Loop.StationServices;
            if (!services.StationStorages.TryGetValue(_dockedStationId, out var storage)) return;
            if (!storage.Stacks.TryGetValue(_selectedStationResource, out var stack)) return;

            var before = _stateStore.Current;
            _stateStore.Dispatch(new TransferToShipAction(_dockedStationId, _selectedStationResource, qty, stack.VolumePerUnit));

            if (ReferenceEquals(before, _stateStore.Current))
            {
                if (_errorLabel != null) _errorLabel.text = "Cargo Full";
            }
            else
            {
                _eventBus?.Publish(new CargoTransferredEvent(_selectedStationResource, qty, false));
            }
        }
    }
}
