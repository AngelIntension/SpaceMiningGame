using System.Collections.Immutable;
using UnityEngine;
using VContainer;
using VoidHarvest.Core.EventBus;
using VoidHarvest.Core.State;
using VoidHarvest.Features.StationServices.Data;
using VoidHarvest.Features.StationServices.Systems;

namespace VoidHarvest.Features.StationServices.Views
{
    /// <summary>
    /// Ticks refining job timers each frame. Dispatches CompleteRefiningJobAction
    /// when elapsed time exceeds duration.
    /// See Spec 006: Station Services, R-004.
    /// </summary>
    public sealed class RefiningJobTicker : MonoBehaviour
    {
        private IStateStore _stateStore;
        private IEventBus _eventBus;

        [Inject]
        public void Construct(IStateStore stateStore, IEventBus eventBus)
        {
            _stateStore = stateStore;
            _eventBus = eventBus;
        }

        private void Update()
        {
            if (_stateStore == null) return;

            float currentTime = Time.realtimeSinceStartup;
            var services = _stateStore.Current.Loop.StationServices;

            foreach (var stationJobs in services.RefiningJobs)
            {
                int stationId = stationJobs.Key;
                var jobs = stationJobs.Value;

                for (int i = 0; i < jobs.Length; i++)
                {
                    var job = jobs[i];
                    if (job.Status != RefiningJobStatus.Active) continue;
                    if (currentTime < job.StartTime + job.TotalDuration) continue;

                    // Job completed — calculate outputs
                    var seed = (uint)job.JobId.GetHashCode();
                    var random = new Unity.Mathematics.Random(seed == 0 ? 1u : seed);
                    var outputs = RefiningMath.CalculateOutputs(job.OutputConfigs, job.InputQuantity, ref random);

                    _stateStore.Dispatch(new CompleteRefiningJobAction(stationId, job.JobId, outputs));
                    _eventBus?.Publish(new RefiningJobCompletedEvent(stationId, job.JobId));
                }
            }
        }
    }
}
