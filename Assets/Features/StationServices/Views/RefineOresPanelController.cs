using System.Collections.Immutable;
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
    /// Controller for the refine ores panel. Manages job queue, start job, progress display.
    /// See Spec 006 US3: Refine Ores.
    /// </summary>
    public sealed class RefineOresPanelController : MonoBehaviour
    {
        private IStateStore _stateStore;
        private IEventBus _eventBus;
        private StationServicesConfigMap _configMap;
        private int _dockedStationId;

        private VisualElement _root;
        private DropdownField _oreDropdown;
        private SliderInt _quantitySlider;
        private Label _quantityLabel;
        private Label _costLabel;
        private Label _affordableHint;
        private Label _outputsPreview;
        private Label _errorLabel;
        private Button _btnStartJob;
        private ScrollView _activeJobsList;
        private ScrollView _completedJobsList;
        private Button _btnBack;

        private string _selectedOreId;
        private int _selectedCostPerUnit;
        private CancellationTokenSource _stateCts;

        // Callback for when a completed job is clicked (opens summary)
        public System.Action<RefiningJobState> OnCompletedJobClicked;

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

            _oreDropdown = _root.Q<DropdownField>("ore-dropdown");
            _quantitySlider = _root.Q<SliderInt>("refine-quantity-slider");
            _quantityLabel = _root.Q<Label>("refine-quantity-label");
            _costLabel = _root.Q<Label>("refine-cost");
            _affordableHint = _root.Q<Label>("refine-affordable-hint");
            _outputsPreview = _root.Q<Label>("refine-outputs-preview");
            _errorLabel = _root.Q<Label>("refine-error");
            _btnStartJob = _root.Q<Button>("btn-start-job");
            _activeJobsList = _root.Q<ScrollView>("active-jobs-list");
            _completedJobsList = _root.Q<ScrollView>("completed-jobs-list");
            _btnBack = _root.Q<Button>("btn-back");

            _oreDropdown?.RegisterValueChangedCallback(evt => OnOreSelected(evt.newValue));
            _quantitySlider?.RegisterValueChangedCallback(evt =>
            {
                if (_quantityLabel != null) _quantityLabel.text = $"Qty: {evt.newValue}";
                UpdatePreview();
            });
            _btnStartJob?.RegisterCallback<ClickEvent>(_ => OnStartJob());

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

            var services = _stateStore.Current.Loop.StationServices;
            PopulateOreDropdown(services);
            PopulateJobLists(services);
            UpdatePreview();
        }

        private void PopulateOreDropdown(StationServicesState services)
        {
            if (_oreDropdown == null) return;

            var choices = new System.Collections.Generic.List<string>();
            if (services.StationStorages.TryGetValue(_dockedStationId, out var storage))
            {
                foreach (var kvp in storage.Stacks)
                    choices.Add(kvp.Key);
            }
            _oreDropdown.choices = choices;
        }

        private void PopulateJobLists(StationServicesState services)
        {
            _activeJobsList?.Clear();
            _completedJobsList?.Clear();

            if (!services.RefiningJobs.TryGetValue(_dockedStationId, out var jobs)) return;

            float currentTime = Time.time;
            foreach (var job in jobs)
            {
                if (job.Status == RefiningJobStatus.Active)
                {
                    float progress = job.Progress(currentTime);
                    float remaining = job.RemainingTime(currentTime);
                    var label = new Label { text = $"{job.OreId} x{job.InputQuantity} — {progress:P0} ({remaining:F0}s)" };
                    label.AddToClassList("item-row");
                    _activeJobsList?.Add(label);
                }
                else if (job.Status == RefiningJobStatus.Completed)
                {
                    var btn = new Button { text = $"{job.OreId} x{job.InputQuantity} — COMPLETE (click to review)" };
                    btn.AddToClassList("item-row");
                    var capturedJob = job;
                    btn.RegisterCallback<ClickEvent>(_ => OnCompletedJobClicked?.Invoke(capturedJob));
                    _completedJobsList?.Add(btn);
                }
            }
        }

        private void OnOreSelected(string oreId)
        {
            _selectedOreId = oreId;
            // TODO: Look up OreDefinition for cost and output configs
            _selectedCostPerUnit = 5; // Placeholder

            var services = _stateStore.Current.Loop.StationServices;
            if (services.StationStorages.TryGetValue(_dockedStationId, out var storage)
                && storage.Stacks.TryGetValue(oreId, out var stack))
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
            if (_selectedOreId == null) return;
            int qty = _quantitySlider?.value ?? 1;
            int cost = RefiningMath.CalculateJobCost(qty, _selectedCostPerUnit);

            if (_costLabel != null) _costLabel.text = $"Cost: {cost} credits";

            int credits = _stateStore.Current.Loop.StationServices.Credits;
            if (_affordableHint != null)
            {
                if (credits < cost && _selectedCostPerUnit > 0)
                {
                    int maxAffordable = credits / _selectedCostPerUnit;
                    _affordableHint.text = $"Can afford: {maxAffordable}";
                }
                else
                {
                    _affordableHint.text = "";
                }
            }
        }

        private void OnStartJob()
        {
            if (_selectedOreId == null) return;
            int qty = _quantitySlider?.value ?? 1;
            int cost = RefiningMath.CalculateJobCost(qty, _selectedCostPerUnit);

            var config = _configMap?.GetConfig(_dockedStationId);
            int maxSlots = config != null ? config.MaxConcurrentRefiningSlots : 3;
            float speedMult = config != null ? config.RefiningSpeedMultiplier : 1f;

            // TODO: Get BaseProcessingTimePerUnit from OreDefinition
            float duration = RefiningMath.CalculateJobDuration(qty, 5f, speedMult);

            // TODO: Build output configs from OreDefinition.RefiningOutputs
            var outputConfigs = ImmutableArray<RefiningOutputConfig>.Empty;

            var before = _stateStore.Current;
            _stateStore.Dispatch(new StartRefiningJobAction(
                _dockedStationId, _selectedOreId, qty, cost, duration, outputConfigs, maxSlots, Time.time));

            if (!ReferenceEquals(before, _stateStore.Current))
            {
                var jobs = _stateStore.Current.Loop.StationServices.RefiningJobs[_dockedStationId];
                var lastJob = jobs[jobs.Length - 1];
                _eventBus?.Publish(new RefiningJobStartedEvent(_dockedStationId, lastJob.JobId));
            }
        }
    }
}
