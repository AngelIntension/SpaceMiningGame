using Unity.Entities;
using Unity.Transforms;
using UnityEngine;
using VContainer;
using VoidHarvest.Core.State;
using VoidHarvest.Features.Mining.Data;
using VoidHarvest.Features.Ship.Data;

namespace VoidHarvest.Features.Mining.Views
{
    /// <summary>
    /// Renders mining beam (LineRenderer) from ship to target asteroid.
    /// Reads MiningBeamComponent from ECS for target position, MiningSessionState from StateStore for activation.
    /// See MVP-05: Mining beam and yield.
    /// </summary>
    [RequireComponent(typeof(LineRenderer))]
    public sealed class MiningBeamView : MonoBehaviour
    {
        [SerializeField] private OreTypeDefinition[] oreDefinitions;

        private IStateStore _stateStore;
        private LineRenderer _lineRenderer;

        private EntityManager _entityManager;
        private Entity _shipEntity;
        private bool _ecsReady;

        [Inject]
        public void Construct(IStateStore stateStore)
        {
            _stateStore = stateStore;
        }

        private void Awake()
        {
            _lineRenderer = GetComponent<LineRenderer>();
            _lineRenderer.positionCount = 2;
            _lineRenderer.enabled = false;
        }

        private void LateUpdate()
        {
            if (_stateStore == null) return;

            if (!_ecsReady)
                TryInitializeECS();

            var miningState = _stateStore.Current.Loop.Mining;

            if (!miningState.TargetAsteroidId.HasValue || !miningState.ActiveOreId.HasValue)
            {
                _lineRenderer.enabled = false;
                return;
            }

            // Get target position from ECS
            if (!_ecsReady || !TryGetTargetPosition(out var targetPos))
            {
                _lineRenderer.enabled = false;
                return;
            }

            _lineRenderer.enabled = true;

            string oreId = miningState.ActiveOreId.GetValueOrDefault("");
            var color = FindBeamColor(oreId);
            _lineRenderer.startColor = color;
            _lineRenderer.endColor = color;

            _lineRenderer.SetPosition(0, transform.position);
            _lineRenderer.SetPosition(1, targetPos);
        }

        private void TryInitializeECS()
        {
            var world = World.DefaultGameObjectInjectionWorld;
            if (world == null || !world.IsCreated) return;

            _entityManager = world.EntityManager;

            var query = _entityManager.CreateEntityQuery(typeof(PlayerControlledTag));
            if (query.CalculateEntityCount() > 0)
            {
                var entities = query.ToEntityArray(Unity.Collections.Allocator.Temp);
                _shipEntity = entities[0];
                entities.Dispose();
                _ecsReady = true;
            }
        }

        private bool TryGetTargetPosition(out Vector3 position)
        {
            position = Vector3.zero;

            if (!_entityManager.HasComponent<MiningBeamComponent>(_shipEntity))
                return false;

            var beam = _entityManager.GetComponentData<MiningBeamComponent>(_shipEntity);
            if (!beam.Active || beam.TargetAsteroid == Entity.Null)
                return false;

            if (!_entityManager.Exists(beam.TargetAsteroid) ||
                !_entityManager.HasComponent<LocalTransform>(beam.TargetAsteroid))
                return false;

            var targetTransform = _entityManager.GetComponentData<LocalTransform>(beam.TargetAsteroid);
            position = targetTransform.Position;
            return true;
        }

        private Color FindBeamColor(string oreId)
        {
            if (oreDefinitions == null) return Color.white;

            foreach (var def in oreDefinitions)
            {
                if (def != null && def.OreId == oreId)
                    return def.BeamColor;
            }
            return Color.white;
        }
    }
}
