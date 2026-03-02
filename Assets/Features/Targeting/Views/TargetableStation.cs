using UnityEngine;
using VContainer;
using VoidHarvest.Core.Extensions;
using VoidHarvest.Core.State;

namespace VoidHarvest.Features.Targeting.Views
{
    /// <summary>
    /// MonoBehaviour implementing ITargetable for station GameObjects.
    /// Placed alongside DockingPortComponent on station prefabs.
    /// See Spec 007: In-Flight Targeting.
    /// </summary>
    public sealed class TargetableStation : MonoBehaviour, ITargetable
    {
        [SerializeField] private int stationId;

        private string _displayName = "Unknown Station";
        private IStateStore _stateStore;

        public int TargetId => gameObject.GetInstanceID();
        public string DisplayName => _displayName;
        public string TypeLabel => "Station";
        public TargetType TargetType => TargetType.Station;

        [Inject]
        public void Construct(IStateStore stateStore)
        {
            _stateStore = stateStore;
        }

        private void Start()
        {
            if (_stateStore == null) return;

            var stations = _stateStore.Current.World.Stations;
            for (int i = 0; i < stations.Length; i++)
            {
                if (stations[i].Id == stationId)
                {
                    _displayName = stations[i].Name;
                    return;
                }
            }
        }
    }
}
