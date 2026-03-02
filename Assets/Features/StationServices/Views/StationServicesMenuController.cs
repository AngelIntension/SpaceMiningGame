using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.UIElements;
using VContainer;
using VoidHarvest.Core.EventBus;
using VoidHarvest.Core.State;
using VoidHarvest.Core.EventBus.Events;
using VoidHarvest.Features.Docking.Data;
using VoidHarvest.Features.Input.Views;
using VoidHarvest.Features.StationServices.Data;

namespace VoidHarvest.Features.StationServices.Views
{
    /// <summary>
    /// Controls the station services menu UI. Opens on dock completion,
    /// closes on undock start. Manages sub-panel navigation and controllers.
    /// See Spec 006: Station Services, FR-001 through FR-006.
    /// </summary>
    public sealed class StationServicesMenuController : MonoBehaviour
    {
        [SerializeField] private UIDocument uiDocument;

        private IStateStore _stateStore;
        private IEventBus _eventBus;
        private StationServicesConfigMap _configMap;
        private CancellationTokenSource _eventCts;

        private VisualElement _root;
        private Label _stationName;
        private Label _stationType;
        private Button _tabCargo;
        private Button _tabMarket;
        private Button _tabRefinery;
        private Button _tabRepair;
        private Button _undockButton;

        private VisualElement _panelCargo;
        private VisualElement _panelMarket;
        private VisualElement _panelRefinery;
        private VisualElement _panelRepair;
        private VisualElement _summaryOverlay;

        private CargoTransferPanelController _cargoController;
        private SellResourcesPanelController _sellController;
        private RefineOresPanelController _refineController;
        private BasicRepairPanelController _repairController;
        private RefiningJobSummaryController _summaryController;
        private CreditBalanceIndicator _creditIndicator;

        private int _dockedStationId;
        private Button _activeTab;

        // Drag state
        private VisualElement _header;
        private bool _isDragging;
        private Vector2 _dragOffset;

        private InputBridge _inputBridge;

        [Inject]
        public void Construct(
            IStateStore stateStore,
            IEventBus eventBus,
            StationServicesConfigMap configMap)
        {
            _stateStore = stateStore;
            _eventBus = eventBus;
            _configMap = configMap;
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

            // Header
            _stationName = _root.Q<Label>("station-name");
            _stationType = _root.Q<Label>("station-type");
            // Tabs
            _tabCargo = _root.Q<Button>("tab-cargo");
            _tabMarket = _root.Q<Button>("tab-market");
            _tabRefinery = _root.Q<Button>("tab-refinery");
            _tabRepair = _root.Q<Button>("tab-repair");
            _undockButton = _root.Q<Button>("undock-button");

            // Panels
            _panelCargo = _root.Q<VisualElement>("panel-cargo");
            _panelMarket = _root.Q<VisualElement>("panel-market");
            _panelRefinery = _root.Q<VisualElement>("panel-refinery");
            _panelRepair = _root.Q<VisualElement>("panel-repair");
            _summaryOverlay = _root.Q<VisualElement>("refining-summary-overlay");

            // Tab callbacks
            _tabCargo?.RegisterCallback<ClickEvent>(_ => ShowPanel("Cargo"));
            _tabMarket?.RegisterCallback<ClickEvent>(_ => ShowPanel("Market"));
            _tabRefinery?.RegisterCallback<ClickEvent>(_ => ShowPanel("Refinery"));
            _tabRepair?.RegisterCallback<ClickEvent>(_ => ShowPanel("Repair"));
            _undockButton?.RegisterCallback<ClickEvent>(_ => OnUndockClicked());

            // Header drag-to-move
            _header = _root.Q<VisualElement>("services-header");
            if (_header != null)
            {
                _header.RegisterCallback<PointerDownEvent>(OnHeaderPointerDown);
                _header.RegisterCallback<PointerMoveEvent>(OnHeaderPointerMove);
                _header.RegisterCallback<PointerUpEvent>(OnHeaderPointerUp);
            }

            // Block scroll-wheel zoom when pointer is over the menu
            _root.RegisterCallback<PointerEnterEvent>(_ => _inputBridge?.SetPointerOverScrollUI(true));
            _root.RegisterCallback<PointerLeaveEvent>(_ => _inputBridge?.SetPointerOverScrollUI(false));

            _root.style.display = DisplayStyle.None;
        }

        private void Start()
        {
            _inputBridge = FindObjectOfType<InputBridge>();

            // Get sub-controllers from sibling components
            _cargoController = GetComponent<CargoTransferPanelController>();
            _sellController = GetComponent<SellResourcesPanelController>();
            _refineController = GetComponent<RefineOresPanelController>();
            _repairController = GetComponent<BasicRepairPanelController>();
            _summaryController = GetComponent<RefiningJobSummaryController>();
            _creditIndicator = GetComponent<CreditBalanceIndicator>();

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
            CleanupControllers();
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
            _dockedStationId = stationId;

            // Station info
            var stations = _stateStore?.Current.World.Stations;
            var station = stations?.FirstOrDefault(s => s.Id == stationId);

            if (_stationName != null)
                _stationName.text = station?.Name ?? $"Station {stationId}";

            if (_stationType != null)
                _stationType.text = station != null ? FormatStationType(station) : "Unknown Type";

            // Configure tab availability based on station services
            ConfigureTabAvailability(station);

            // Initialize sub-controllers
            InitializeControllers(stationId);

            // Reset position to center
            _root.style.left = new StyleLength(new Length(50, LengthUnit.Percent));
            _root.style.top = new StyleLength(new Length(50, LengthUnit.Percent));
            _root.style.translate = new Translate(Length.Percent(-50), Length.Percent(-50));

            // Default to Cargo tab
            ShowPanel("Cargo");

            _root.style.display = DisplayStyle.Flex;
        }

        public void Close()
        {
            if (_root == null) return;
            _root.style.display = DisplayStyle.None;
            _activeTab = null;
            CleanupControllers();
        }

        public bool IsOpen => _root != null && _root.style.display == DisplayStyle.Flex;

        private void ConfigureTabAvailability(StationData station)
        {
            var services = station?.AvailableServices ?? ImmutableArray<string>.Empty;

            SetTabEnabled(_tabCargo, services.Contains("Cargo"));
            SetTabEnabled(_tabMarket, services.Contains("Market"));
            SetTabEnabled(_tabRefinery, services.Contains("Refinery"));
            SetTabEnabled(_tabRepair, services.Contains("Repair"));
        }

        private static void SetTabEnabled(Button tab, bool enabled)
        {
            if (tab == null) return;
            tab.style.display = enabled ? DisplayStyle.Flex : DisplayStyle.None;
        }

        private void InitializeControllers(int stationId)
        {
            _creditIndicator?.Initialize(_root.Q<Label>("credit-balance"));

            if (_cargoController != null && _panelCargo != null)
                _cargoController.Initialize(_panelCargo, stationId);

            if (_sellController != null && _panelMarket != null)
                _sellController.Initialize(_panelMarket, stationId);

            if (_refineController != null && _panelRefinery != null)
            {
                _refineController.Initialize(_panelRefinery, stationId);
                _refineController.OnCompletedJobClicked = job =>
                {
                    _summaryController?.Open(job, stationId);
                };
            }

            if (_repairController != null && _panelRepair != null)
                _repairController.Initialize(_panelRepair, stationId);

            if (_summaryController != null && _summaryOverlay != null)
                _summaryController.Initialize(_summaryOverlay);
        }

        private void CleanupControllers()
        {
            _cargoController?.Cleanup();
            _sellController?.Cleanup();
            _refineController?.Cleanup();
            _repairController?.Cleanup();
            _creditIndicator?.Cleanup();
        }

        private void ShowPanel(string panelName)
        {
            HideAllPanels();
            ClearActiveTab();

            switch (panelName)
            {
                case "Cargo":
                    Show(_panelCargo);
                    SetActiveTab(_tabCargo);
                    break;
                case "Market":
                    Show(_panelMarket);
                    SetActiveTab(_tabMarket);
                    break;
                case "Refinery":
                    Show(_panelRefinery);
                    SetActiveTab(_tabRefinery);
                    break;
                case "Repair":
                    Show(_panelRepair);
                    SetActiveTab(_tabRepair);
                    break;
            }
        }

        private void HideAllPanels()
        {
            Hide(_panelCargo);
            Hide(_panelMarket);
            Hide(_panelRefinery);
            Hide(_panelRepair);
        }

        private void ClearActiveTab()
        {
            _tabCargo?.RemoveFromClassList("services-tab--active");
            _tabMarket?.RemoveFromClassList("services-tab--active");
            _tabRefinery?.RemoveFromClassList("services-tab--active");
            _tabRepair?.RemoveFromClassList("services-tab--active");
            _activeTab = null;
        }

        private void SetActiveTab(Button tab)
        {
            tab?.AddToClassList("services-tab--active");
            _activeTab = tab;
        }

        private static void Show(VisualElement el)
        {
            if (el != null) el.style.display = DisplayStyle.Flex;
        }

        private static void Hide(VisualElement el)
        {
            if (el != null) el.style.display = DisplayStyle.None;
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

        // --- Drag-to-move ---

        private void OnHeaderPointerDown(PointerDownEvent evt)
        {
            if (evt.button != 0) return;
            _isDragging = true;

            // Convert centered CSS positioning to absolute pixel coords on first drag
            var bounds = _root.worldBound;
            _root.style.left = bounds.x;
            _root.style.top = bounds.y;
            _root.style.translate = new Translate(0, 0);

            _dragOffset = new Vector2(evt.position.x - bounds.x, evt.position.y - bounds.y);
            _header.CapturePointer(evt.pointerId);
            evt.StopPropagation();
        }

        private void OnHeaderPointerMove(PointerMoveEvent evt)
        {
            if (!_isDragging) return;

            float newLeft = evt.position.x - _dragOffset.x;
            float newTop = evt.position.y - _dragOffset.y;

            _root.style.left = newLeft;
            _root.style.top = newTop;
            evt.StopPropagation();
        }

        private void OnHeaderPointerUp(PointerUpEvent evt)
        {
            if (!_isDragging) return;
            _isDragging = false;
            _header.ReleasePointer(evt.pointerId);
            evt.StopPropagation();
        }
    }
}
