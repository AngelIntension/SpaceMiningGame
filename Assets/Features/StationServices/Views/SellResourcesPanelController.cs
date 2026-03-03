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
    /// Controller for selling resources from station storage for credits.
    /// See Spec 006 US2: Sell Resources.
    /// </summary>
    public sealed class SellResourcesPanelController : MonoBehaviour
    {
        private IStateStore _stateStore;
        private IEventBus _eventBus;
        private int _dockedStationId;

        private VisualElement _root;
        private ScrollView _itemList;
        private SliderInt _quantitySlider;
        private Label _previewLabel;
        private Label _errorLabel;
        private Button _btnSell;
        private VisualElement _confirmOverlay;
        private Button _btnConfirmSell;
        private Button _btnCancelSell;

        private string _selectedResource;
        private int _selectedBaseValue;
        private CancellationTokenSource _stateCts;
        private StationServicesState _lastServices;
        private InventoryState _lastInventory;

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

            _itemList = _root.Q<ScrollView>("sell-item-list");
            _quantitySlider = _root.Q<SliderInt>("sell-quantity-slider");
            _previewLabel = _root.Q<Label>("sell-preview");
            _errorLabel = _root.Q<Label>("sell-error");
            _btnSell = _root.Q<Button>("btn-sell");
            _confirmOverlay = _root.Q<VisualElement>("sell-confirm-overlay");
            _btnConfirmSell = _root.Q<Button>("btn-confirm-sell");
            _btnCancelSell = _root.Q<Button>("btn-cancel-sell");

            _quantitySlider?.RegisterValueChangedCallback(evt => UpdatePreview());

            _btnSell?.RegisterCallback<ClickEvent>(_ => ShowConfirmation());
            _btnConfirmSell?.RegisterCallback<ClickEvent>(_ => OnConfirmSell());
            _btnCancelSell?.RegisterCallback<ClickEvent>(_ => HideConfirmation());

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
                var svc = evt.CurrentState.Loop.StationServices;
                var inv = evt.CurrentState.Loop.Inventory;
                if (ReferenceEquals(svc, _lastServices) && ReferenceEquals(inv, _lastInventory))
                    continue;
                _lastServices = svc;
                _lastInventory = inv;
                RefreshUI();
            }
        }

        private void RefreshUI()
        {
            if (_stateStore == null || _root == null) return;

            var services = _stateStore.Current.Loop.StationServices;
            _itemList?.Clear();

            if (services.StationStorages.TryGetValue(_dockedStationId, out var storage))
            {
                if (storage.Stacks.Count == 0)
                {
                    if (_errorLabel != null) _errorLabel.text = "No items available for sale";
                }
                else
                {
                    if (_errorLabel != null) _errorLabel.text = "";
                    foreach (var kvp in storage.Stacks)
                    {
                        var btn = new Button { text = $"{OreDefinitionRegistry.GetDisplayName(kvp.Key)}: {kvp.Value.Quantity}" };
                        btn.AddToClassList("item-row");
                        var resId = kvp.Key;
                        if (resId == _selectedResource) btn.AddToClassList("item-row--selected");
                        btn.RegisterCallback<ClickEvent>(_ => SelectResource(resId));
                        _itemList?.Add(btn);
                    }
                }
            }
        }

        private void SelectResource(string resourceId)
        {
            _selectedResource = resourceId;
            // TODO: Look up base value from OreDefinition or RawMaterialDefinition
            // For now, use a default; real implementation will use SO lookup
            _selectedBaseValue = 10;

            if (_itemList != null)
            {
                foreach (var child in _itemList.Children())
                {
                    if (child is Button btn)
                    {
                        var resId = btn.text.Split(':')[0];
                        if (resId == _selectedResource)
                            btn.AddToClassList("item-row--selected");
                        else
                            btn.RemoveFromClassList("item-row--selected");
                    }
                }
            }

            var services = _stateStore.Current.Loop.StationServices;
            if (services.StationStorages.TryGetValue(_dockedStationId, out var storage)
                && storage.Stacks.TryGetValue(resourceId, out var stack))
            {
                if (_quantitySlider != null)
                {
                    _quantitySlider.highValue = Mathf.Max(1, stack.Quantity);
                    _quantitySlider.value = 1;
                }
            }

            UpdatePreview();
        }

        private void UpdatePreview()
        {
            if (_previewLabel == null || _selectedResource == null) return;
            int qty = _quantitySlider?.value ?? 1;
            int total = qty * _selectedBaseValue;
            _previewLabel.text = $"Total: {total} credits";
        }

        private void ShowConfirmation()
        {
            if (_confirmOverlay != null) _confirmOverlay.style.display = DisplayStyle.Flex;
        }

        private void HideConfirmation()
        {
            if (_confirmOverlay != null) _confirmOverlay.style.display = DisplayStyle.None;
        }

        private void OnConfirmSell()
        {
            HideConfirmation();
            if (_selectedResource == null) return;

            int qty = _quantitySlider?.value ?? 1;
            int oldCredits = _stateStore.Current.Loop.StationServices.Credits;

            _stateStore.Dispatch(new SellResourceAction(_dockedStationId, _selectedResource, qty, _selectedBaseValue));

            int newCredits = _stateStore.Current.Loop.StationServices.Credits;
            _eventBus?.Publish(new ResourcesSoldEvent(_selectedResource, qty, newCredits - oldCredits));
            _eventBus?.Publish(new CreditsChangedEvent(oldCredits, newCredits));
        }
    }
}
