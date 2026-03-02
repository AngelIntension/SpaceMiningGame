using UnityEngine;
using UnityEngine.UIElements;
using VContainer;
using VoidHarvest.Core.EventBus;
using VoidHarvest.Core.State;
using VoidHarvest.Features.Mining.Data;
using VoidHarvest.Features.StationServices.Data;

namespace VoidHarvest.Features.StationServices.Views
{
    /// <summary>
    /// Controller for the refining job summary modal. Shows generated materials on completion.
    /// See Spec 006 US3: Refine Ores.
    /// </summary>
    public sealed class RefiningJobSummaryController : MonoBehaviour
    {
        private IStateStore _stateStore;
        private IEventBus _eventBus;

        private VisualElement _overlay;
        private Label _headerLabel;
        private Label _oreInfoLabel;
        private ScrollView _outputsList;
        private Button _btnClose;

        private int _stationId;
        private string _jobId;

        [Inject]
        public void Construct(IStateStore stateStore, IEventBus eventBus)
        {
            _stateStore = stateStore;
            _eventBus = eventBus;
        }

        public void Initialize(VisualElement overlay)
        {
            _overlay = overlay;
            _headerLabel = _overlay.Q<Label>("summary-header");
            _oreInfoLabel = _overlay.Q<Label>("summary-ore-info");
            _outputsList = _overlay.Q<ScrollView>("summary-outputs");
            _btnClose = _overlay.Q<Button>("btn-close-summary");

            _btnClose?.RegisterCallback<ClickEvent>(_ => OnClose());
        }

        public void Open(RefiningJobState job, int stationId)
        {
            if (job == null || job.Status != RefiningJobStatus.Completed) return;

            _stationId = stationId;
            _jobId = job.JobId;

            if (_oreInfoLabel != null)
                _oreInfoLabel.text = $"{OreDefinitionRegistry.GetDisplayName(job.OreId)} x{job.InputQuantity}";

            _outputsList?.Clear();
            foreach (var output in job.GeneratedOutputs)
            {
                var label = new Label { text = $"{OreDefinitionRegistry.GetDisplayName(output.MaterialId)}: {output.Quantity}" };
                label.AddToClassList("item-row");
                _outputsList?.Add(label);
            }

            if (_overlay != null) _overlay.style.display = DisplayStyle.Flex;
        }

        private void OnClose()
        {
            if (_overlay != null) _overlay.style.display = DisplayStyle.None;

            if (_jobId != null)
            {
                _stateStore?.Dispatch(new CollectRefiningJobAction(_stationId, _jobId));
                _eventBus?.Publish(new RefiningJobCollectedEvent(_stationId, _jobId));
                _jobId = null;
            }
        }
    }
}
