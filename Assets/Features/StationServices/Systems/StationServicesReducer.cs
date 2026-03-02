using System.Collections.Immutable;
using VoidHarvest.Core.State;
using VoidHarvest.Features.StationServices.Data;

namespace VoidHarvest.Features.StationServices.Systems
{
    /// <summary>
    /// Pure static reducer for the station services state slice.
    /// Handles single-slice actions. Cross-cutting actions handled in CompositeReducer.
    /// See Spec 006: Station Services.
    /// </summary>
    public static class StationServicesReducer
    {
        public static StationServicesState Reduce(StationServicesState state, IStationServicesAction action)
            => action switch
            {
                SetCreditsAction a => state with { Credits = a.NewBalance },
                InitializeStationStorageAction a => HandleInitializeStorage(state, a),
                AddToStationStorageAction a => HandleAddToStorage(state, a),
                RemoveFromStationStorageAction a => HandleRemoveFromStorage(state, a),
                SellResourceAction a => HandleSellResource(state, a),
                StartRefiningJobAction a => HandleStartRefiningJob(state, a),
                CompleteRefiningJobAction a => HandleCompleteRefiningJob(state, a),
                CollectRefiningJobAction a => HandleCollectRefiningJob(state, a),
                _ => state
            };

        private static StationServicesState HandleInitializeStorage(StationServicesState state, InitializeStationStorageAction a)
        {
            if (state.StationStorages.ContainsKey(a.StationId))
                return state;

            return state with
            {
                StationStorages = state.StationStorages.Add(a.StationId, StationStorageState.Empty)
            };
        }

        private static StationServicesState HandleAddToStorage(StationServicesState state, AddToStationStorageAction a)
        {
            if (!state.StationStorages.TryGetValue(a.StationId, out var storage))
                return state;

            var updated = StationStorageReducer.AddResource(storage, a.ResourceId, a.Quantity, a.VolumePerUnit);
            return state with { StationStorages = state.StationStorages.SetItem(a.StationId, updated) };
        }

        private static StationServicesState HandleRemoveFromStorage(StationServicesState state, RemoveFromStationStorageAction a)
        {
            if (!state.StationStorages.TryGetValue(a.StationId, out var storage))
                return state;

            var updated = StationStorageReducer.RemoveResource(storage, a.ResourceId, a.Quantity);
            return state with { StationStorages = state.StationStorages.SetItem(a.StationId, updated) };
        }

        private static StationServicesState HandleSellResource(StationServicesState state, SellResourceAction a)
        {
            if (a.Quantity <= 0) return state;

            if (!state.StationStorages.TryGetValue(a.StationId, out var storage))
                return state;

            if (!storage.Stacks.TryGetValue(a.ResourceId, out var stack) || stack.Quantity < a.Quantity)
                return state;

            var updatedStorage = StationStorageReducer.RemoveResource(storage, a.ResourceId, a.Quantity);
            int earnedCredits = a.Quantity * a.PricePerUnit;

            return state with
            {
                Credits = state.Credits + earnedCredits,
                StationStorages = state.StationStorages.SetItem(a.StationId, updatedStorage)
            };
        }

        private static StationServicesState HandleStartRefiningJob(StationServicesState state, StartRefiningJobAction a)
        {
            if (a.InputQuantity <= 0) return state;

            // Validate station storage exists and has sufficient ore
            if (!state.StationStorages.TryGetValue(a.StationId, out var storage))
                return state;

            if (!storage.Stacks.TryGetValue(a.OreId, out var oreStack) || oreStack.Quantity < a.InputQuantity)
                return state;

            // Validate sufficient credits
            if (state.Credits < a.TotalCost)
                return state;

            // Validate active job slots
            var jobs = state.RefiningJobs.TryGetValue(a.StationId, out var existingJobs)
                ? existingJobs
                : ImmutableArray<RefiningJobState>.Empty;

            int activeCount = 0;
            foreach (var j in jobs)
            {
                if (j.Status == RefiningJobStatus.Active)
                    activeCount++;
            }

            if (activeCount >= a.MaxActiveSlots)
                return state;

            // Remove ore from storage
            var updatedStorage = StationStorageReducer.RemoveResource(storage, a.OreId, a.InputQuantity);

            // Create new job
            var jobId = System.Guid.NewGuid().ToString("N");
            var newJob = new RefiningJobState(
                jobId, a.OreId, a.InputQuantity, a.StartTime, a.TotalDuration,
                a.TotalCost, RefiningJobStatus.Active, a.OutputConfigs,
                ImmutableArray<MaterialOutput>.Empty
            );

            var updatedJobs = jobs.Add(newJob);

            return state with
            {
                Credits = state.Credits - a.TotalCost,
                StationStorages = state.StationStorages.SetItem(a.StationId, updatedStorage),
                RefiningJobs = state.RefiningJobs.SetItem(a.StationId, updatedJobs)
            };
        }

        private static StationServicesState HandleCompleteRefiningJob(StationServicesState state, CompleteRefiningJobAction a)
        {
            if (!state.RefiningJobs.TryGetValue(a.StationId, out var jobs))
                return state;

            var builder = jobs.ToBuilder();
            for (int i = 0; i < builder.Count; i++)
            {
                if (builder[i].JobId == a.JobId && builder[i].Status == RefiningJobStatus.Active)
                {
                    builder[i] = builder[i] with
                    {
                        Status = RefiningJobStatus.Completed,
                        GeneratedOutputs = a.GeneratedOutputs
                    };
                    return state with
                    {
                        RefiningJobs = state.RefiningJobs.SetItem(a.StationId, builder.ToImmutable())
                    };
                }
            }

            return state;
        }

        private static StationServicesState HandleCollectRefiningJob(StationServicesState state, CollectRefiningJobAction a)
        {
            if (!state.RefiningJobs.TryGetValue(a.StationId, out var jobs))
                return state;

            if (!state.StationStorages.TryGetValue(a.StationId, out var storage))
                return state;

            RefiningJobState targetJob = null;
            int jobIndex = -1;
            for (int i = 0; i < jobs.Length; i++)
            {
                if (jobs[i].JobId == a.JobId && jobs[i].Status == RefiningJobStatus.Completed)
                {
                    targetJob = jobs[i];
                    jobIndex = i;
                    break;
                }
            }

            if (targetJob == null) return state;

            // Add generated outputs to station storage
            var updatedStorage = storage;
            foreach (var output in targetJob.GeneratedOutputs)
            {
                if (output.Quantity > 0)
                    updatedStorage = StationStorageReducer.AddResource(updatedStorage, output.MaterialId, output.Quantity, 0f);
            }

            // Remove job from list
            var updatedJobs = jobs.RemoveAt(jobIndex);

            return state with
            {
                StationStorages = state.StationStorages.SetItem(a.StationId, updatedStorage),
                RefiningJobs = state.RefiningJobs.SetItem(a.StationId, updatedJobs)
            };
        }
    }
}
