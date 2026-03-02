using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.UIElements;
using VContainer;
using VoidHarvest.Core.EventBus;
using VoidHarvest.Core.State;
using VoidHarvest.Features.StationServices.Data;
using VoidHarvest.Features.StationServices.Systems;

namespace VoidHarvest.Features.StationServices.Views
{
    /// <summary>
    /// Controller for the basic repair panel. One-click hull repair for credits.
    /// See Spec 006 US4: Basic Repair.
    /// </summary>
    public sealed class BasicRepairPanelController : MonoBehaviour
    {
        private IStateStore _stateStore;
        private IEventBus _eventBus;
        private StationServicesConfigMap _configMap;
        private int _dockedStationId;

        private VisualElement _root;
        private Label _hullLabel;
        private ProgressBar _hullBar;
        private Label _costLabel;
        private Label _statusLabel;
        private Button _btnRepair;
        private Button _btnBack;
        private CancellationTokenSource _stateCts;

        [Inject]
        public void Construct(IStateStore stateStore, IEventBus eventBus, StationServicesConfigMap configMap)
        {
            _stateStore = stateStore;
            _eventBus = eventBus;
            _configMap = configMap;
        }

        public void Initialize(VisualElement root, int stationId)
        {
            _root = root;
            _dockedStationId = stationId;

            _hullLabel = _root.Q<Label>("hull-label");
            _hullBar = _root.Q<ProgressBar>("hull-bar");
            _costLabel = _root.Q<Label>("repair-cost");
            _statusLabel = _root.Q<Label>("repair-status");
            _btnRepair = _root.Q<Button>("btn-repair");
            _btnBack = _root.Q<Button>("btn-back");

            _btnRepair?.RegisterCallback<ClickEvent>(_ => OnRepairClicked());

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

            float integrity = _stateStore.Current.ActiveShipPhysics.HullIntegrity;
            int credits = _stateStore.Current.Loop.StationServices.Credits;

            var config = _configMap?.GetConfig(_dockedStationId);
            int repairCostPerHP = config != null ? config.RepairCostPerHP : 100;

            int cost = RepairMath.CalculateRepairCost(integrity, repairCostPerHP);

            if (_hullLabel != null) _hullLabel.text = $"Hull Integrity: {integrity:P0}";
            if (_hullBar != null) _hullBar.value = integrity * 100f;
            if (_costLabel != null) _costLabel.text = $"Cost: {cost} credits";

            bool isFullHealth = integrity >= 1.0f;
            bool canAfford = credits >= cost;

            if (_btnRepair != null) _btnRepair.SetEnabled(!isFullHealth && canAfford);

            if (_statusLabel != null)
            {
                if (isFullHealth)
                    _statusLabel.text = "Hull integrity is at maximum";
                else if (!canAfford)
                    _statusLabel.text = "Insufficient credits";
                else
                    _statusLabel.text = "";
            }
        }

        private void OnRepairClicked()
        {
            float integrity = _stateStore.Current.ActiveShipPhysics.HullIntegrity;
            var config = _configMap?.GetConfig(_dockedStationId);
            int repairCostPerHP = config != null ? config.RepairCostPerHP : 100;
            int cost = RepairMath.CalculateRepairCost(integrity, repairCostPerHP);
            int oldCredits = _stateStore.Current.Loop.StationServices.Credits;

            _stateStore.Dispatch(new RepairShipAction(cost, 1.0f));

            int newCredits = _stateStore.Current.Loop.StationServices.Credits;
            _eventBus?.Publish(new ShipRepairedEvent(cost, 1.0f));
            _eventBus?.Publish(new CreditsChangedEvent(oldCredits, newCredits));
        }
    }
}
