using System.Linq;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.UIElements;
using VContainer;
using VoidHarvest.Core.EventBus;
using VoidHarvest.Core.EventBus.Events;
using VoidHarvest.Core.State;
using VoidHarvest.Features.Docking.Data;

namespace VoidHarvest.Features.StationServices.Views
{
    /// <summary>
    /// Controls the station services menu UI. Opens on dock completion,
    /// closes on undock start. View layer only — no game state.
    /// See Spec 006: Station Services.
    /// </summary>
    public sealed class StationServicesMenuController : MonoBehaviour
    {
        [SerializeField] private UIDocument uiDocument;

        private IStateStore _stateStore;
        private IEventBus _eventBus;
        private CancellationTokenSource _eventCts;

        private VisualElement _root;
        private Label _stationName;
        private Label _stationType;
        private Label _placeholder;
        private Button _tabRefinery;
        private Button _tabMarket;
        private Button _tabRepair;
        private Button _tabCargo;
        private Button _undockButton;
        private Button _activeTab;

        [Inject]
        public void Construct(IStateStore stateStore, IEventBus eventBus)
        {
            _stateStore = stateStore;
            _eventBus = eventBus;
        }

        private void OnEnable()
        {
            if (uiDocument == null) return;

            var rootVisual = uiDocument.rootVisualElement;
            _root = rootVisual.Q<VisualElement>("services-root");
            if (_root == null)
            {
                Debug.LogError("[StationServicesMenu] Could not find 'services-root' in UXML.");
                return;
            }

            _stationName = _root.Q<Label>("station-name");
            _stationType = _root.Q<Label>("station-type");
            _placeholder = _root.Q<Label>("services-placeholder");
            _tabRefinery = _root.Q<Button>("tab-refinery");
            _tabMarket = _root.Q<Button>("tab-market");
            _tabRepair = _root.Q<Button>("tab-repair");
            _tabCargo = _root.Q<Button>("tab-cargo");
            _undockButton = _root.Q<Button>("undock-button");

            _tabRefinery?.RegisterCallback<ClickEvent>(_ => OnTabClicked(_tabRefinery, "Refinery"));
            _tabMarket?.RegisterCallback<ClickEvent>(_ => OnTabClicked(_tabMarket, "Market"));
            _tabRepair?.RegisterCallback<ClickEvent>(_ => OnTabClicked(_tabRepair, "Repair"));
            _tabCargo?.RegisterCallback<ClickEvent>(_ => OnTabClicked(_tabCargo, "Cargo"));
            _undockButton?.RegisterCallback<ClickEvent>(_ => OnUndockClicked());

            _root.style.display = DisplayStyle.None;
        }

        private void Start()
        {
            if (_eventBus != null)
            {
                _eventCts = new CancellationTokenSource();
                ListenForDockingCompleted(_eventCts.Token).Forget();
                ListenForUndockingStarted(_eventCts.Token).Forget();
            }
        }

        private void OnDestroy()
        {
            _eventCts?.Cancel();
            _eventCts?.Dispose();
        }

        private async UniTaskVoid ListenForDockingCompleted(CancellationToken ct)
        {
            await foreach (var evt in _eventBus.Subscribe<DockingCompletedEvent>().WithCancellation(ct))
            {
                Open(evt.StationId);
            }
        }

        private async UniTaskVoid ListenForUndockingStarted(CancellationToken ct)
        {
            await foreach (var evt in _eventBus.Subscribe<UndockingStartedEvent>().WithCancellation(ct))
            {
                Close();
            }
        }

        private void Open(int stationId)
        {
            if (_root == null) return;

            // Query station info from world state
            var stations = _stateStore?.Current.World.Stations;
            var station = stations?.FirstOrDefault(s => s.Id == stationId);

            if (_stationName != null)
                _stationName.text = station?.Name ?? $"Station {stationId}";

            if (_stationType != null)
                _stationType.text = station != null ? FormatStationType(station) : "Unknown Type";

            // Default to first tab
            OnTabClicked(_tabRefinery, "Refinery");

            _root.style.display = DisplayStyle.Flex;
        }

        /// <summary>
        /// Closes the station services menu.
        /// </summary>
        public void Close()
        {
            if (_root == null) return;
            _root.style.display = DisplayStyle.None;
            _activeTab = null;
        }

        /// <summary>
        /// Whether the services menu is currently visible.
        /// </summary>
        public bool IsOpen => _root != null && _root.style.display == DisplayStyle.Flex;

        private void OnTabClicked(Button tab, string tabName)
        {
            // Clear active state from all tabs
            _tabRefinery?.RemoveFromClassList("services-tab--active");
            _tabMarket?.RemoveFromClassList("services-tab--active");
            _tabRepair?.RemoveFromClassList("services-tab--active");
            _tabCargo?.RemoveFromClassList("services-tab--active");

            // Set active tab
            tab?.AddToClassList("services-tab--active");
            _activeTab = tab;

            // Update placeholder text
            if (_placeholder != null)
                _placeholder.text = $"{tabName} — Coming Soon";
        }

        private void OnUndockClicked()
        {
            if (_stateStore == null) return;

            var dockingState = _stateStore.Current.Loop.Docking;
            int stationId = dockingState.TargetStationId.GetValueOrDefault(-1);

            _stateStore.Dispatch(new BeginUndockingAction());
            _eventBus?.Publish(new UndockingStartedEvent(stationId));
        }

        private static string FormatStationType(StationData station)
        {
            if (station.AvailableServices.IsDefaultOrEmpty)
                return "Outpost";

            return string.Join(" / ", station.AvailableServices);
        }
    }
}
