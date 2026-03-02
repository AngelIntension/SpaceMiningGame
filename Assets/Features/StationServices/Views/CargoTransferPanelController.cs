using System.Linq;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.UIElements;
using VContainer;
using VoidHarvest.Core.EventBus;
using VoidHarvest.Core.State;
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
        private Label _quantityLabel;
        private Label _shipVolume;
        private Label _errorLabel;
        private Button _btnToStation;
        private Button _btnToShip;
        private Button _btnBack;

        private string _selectedShipResource;
        private string _selectedStationResource;
        private CancellationTokenSource _stateCts;

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
            _quantityLabel = _root.Q<Label>("quantity-label");
            _shipVolume = _root.Q<Label>("ship-volume");
            _errorLabel = _root.Q<Label>("cargo-error");
            _btnToStation = _root.Q<Button>("btn-to-station");
            _btnToShip = _root.Q<Button>("btn-to-ship");
            _btnBack = _root.Q<Button>("btn-back");

            _quantitySlider?.RegisterValueChangedCallback(evt =>
            {
                if (_quantityLabel != null) _quantityLabel.text = $"Qty: {evt.newValue}";
            });

            _btnToStation?.RegisterCallback<ClickEvent>(_ => OnTransferToStation());
            _btnToShip?.RegisterCallback<ClickEvent>(_ => OnTransferToShip());

            _stateCts?.Cancel();
            _stateCts = new CancellationTokenSource();
            ListenForStateChanges(_stateCts.Token).Forget();

            RefreshUI();
        }

        public Button BackButton => _btnBack;

        public void Cleanup()
        {
            _stateCts?.Cancel();
            _stateCts?.Dispose();
            _stateCts = null;
        }

        private async UniTaskVoid ListenForStateChanges(CancellationToken ct)
        {
            await foreach (var _ in _stateStore.OnStateChanged.WithCancellation(ct))
            {
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
                var btn = new Button { text = $"{kvp.Key}: {kvp.Value.Quantity}" };
                btn.AddToClassList("item-row");
                var resId = kvp.Key;
                btn.RegisterCallback<ClickEvent>(_ => { _selectedShipResource = resId; _selectedStationResource = null; UpdateSliderMax(); });
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
                    var btn = new Button { text = $"{kvp.Key}: {kvp.Value.Quantity}" };
                    btn.AddToClassList("item-row");
                    var resId = kvp.Key;
                    btn.RegisterCallback<ClickEvent>(_ => { _selectedStationResource = resId; _selectedShipResource = null; UpdateSliderMax(); });
                    _stationItems?.Add(btn);
                }
            }

            if (_errorLabel != null) _errorLabel.text = "";
        }

        private void UpdateSliderMax()
        {
            if (_quantitySlider == null) return;
            var state = _stateStore.Current;

            if (_selectedShipResource != null && state.Loop.Inventory.Stacks.TryGetValue(_selectedShipResource, out var shipStack))
            {
                _quantitySlider.highValue = Mathf.Max(1, shipStack.Quantity);
                _quantitySlider.value = Mathf.Min(_quantitySlider.value, _quantitySlider.highValue);
            }
            else if (_selectedStationResource != null
                && state.Loop.StationServices.StationStorages.TryGetValue(_dockedStationId, out var storage)
                && storage.Stacks.TryGetValue(_selectedStationResource, out var stationStack))
            {
                _quantitySlider.highValue = Mathf.Max(1, stationStack.Quantity);
                _quantitySlider.value = Mathf.Min(_quantitySlider.value, _quantitySlider.highValue);
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
