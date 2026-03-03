using UnityEngine;
using VContainer;
using VoidHarvest.Core.Extensions;
using VoidHarvest.Core.State;
using VoidHarvest.Features.Station.Data;

namespace VoidHarvest.Features.Targeting.Views
{
    /// <summary>
    /// MonoBehaviour implementing ITargetable for station GameObjects.
    /// Placed alongside DockingPortComponent on station prefabs.
    /// See Spec 007: In-Flight Targeting, Spec 009: Data-Driven World Config (FR-024).
    /// </summary>
    public sealed class TargetableStation : MonoBehaviour, ITargetable
    {
        [SerializeField] private StationDefinition stationDefinition;

        private int _stationId;
        private string _displayName = "Unknown Station";
        private IStateStore _stateStore;

        public int StationId => _stationId;
        public int TargetId => gameObject.GetInstanceID();
        public string DisplayName => _displayName;
        public string TypeLabel => "Station";
        public TargetType TargetType => TargetType.Station;

        [Inject]
        public void Construct(IStateStore stateStore)
        {
            _stateStore = stateStore;
        }

        private void Awake()
        {
            if (stationDefinition != null)
            {
                _stationId = stationDefinition.StationId;
                _displayName = stationDefinition.DisplayName ?? "Unknown Station";
            }
        }
    }
}
