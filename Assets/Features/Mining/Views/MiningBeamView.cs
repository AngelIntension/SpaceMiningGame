using Unity.Entities;
using Unity.Transforms;
using UnityEngine;
using VContainer;
using VoidHarvest.Core.State;
using VoidHarvest.Features.Mining.Data;
using VoidHarvest.Features.Mining.Systems;
using VoidHarvest.Features.Ship.Data;

namespace VoidHarvest.Features.Mining.Views
{
    /// <summary>
    /// Renders mining beam (LineRenderer) from ship to target asteroid with pulsing width,
    /// ore-colored impact sparks, and heat shimmer at mining arm.
    /// Reads MiningBeamComponent from ECS for target position, MiningSessionState from StateStore for activation.
    /// See FR-006: Pulsing beam, FR-007: Impact sparks, FR-008: Heat shimmer.
    /// </summary>
    [RequireComponent(typeof(LineRenderer))]
    public sealed class MiningBeamView : MonoBehaviour
    {
        [SerializeField] private OreTypeDefinition[] oreDefinitions;

        private IStateStore _stateStore;
        private MiningVFXConfig _vfxConfig;
        private LineRenderer _lineRenderer;

        // Spark particle system at impact point
        private ParticleSystem _sparkSystem;
        private ParticleSystem.EmissionModule _sparkEmission;
        private ParticleSystem.MainModule _sparkMain;

        // Heat shimmer at mining arm
        private ParticleSystem _shimmerSystem;

        // Mining arm origin transform (child of ship)
        private Transform _miningArmOrigin;

        private EntityManager _entityManager;
        private Entity _shipEntity;
        private bool _ecsReady;
        private bool _effectsActive;
        private float _beamTime;

        [Inject]
        public void Construct(IStateStore stateStore, MiningVFXConfig vfxConfig)
        {
            _stateStore = stateStore;
            _vfxConfig = vfxConfig;
        }

        private void Awake()
        {
            _lineRenderer = GetComponent<LineRenderer>();
            _lineRenderer.positionCount = 2;
            _lineRenderer.enabled = false;

            CreateSparkSystem();
            CreateShimmerSystem();
        }

        private void Start()
        {
            // Find mining arm origin on the ship hierarchy
            _miningArmOrigin = transform.FindChildRecursive("MiningArmOrigin");
        }

        private void LateUpdate()
        {
            if (_stateStore == null) return;

            if (!_ecsReady)
                TryInitializeECS();

            var miningState = _stateStore.Current.Loop.Mining;

            if (!miningState.TargetAsteroidId.HasValue || !miningState.ActiveOreId.HasValue)
            {
                if (_effectsActive)
                    StopAllEffects();
                _lineRenderer.enabled = false;
                return;
            }

            // Get target position from ECS
            if (!_ecsReady || !TryGetTargetPosition(out var targetPos))
            {
                if (_effectsActive)
                    StopAllEffects();
                _lineRenderer.enabled = false;
                return;
            }

            _lineRenderer.enabled = true;
            _effectsActive = true;
            _beamTime += Time.deltaTime;

            string oreId = miningState.ActiveOreId.GetValueOrDefault("");
            var beamColor = FindBeamColor(oreId);

            // --- Pulsing beam width ---
            float pulseSpeed = _vfxConfig != null ? _vfxConfig.BeamPulseSpeed : 3f;
            float pulseAmplitude = _vfxConfig != null ? _vfxConfig.BeamPulseAmplitude : 0.3f;
            float baseWidth = _vfxConfig != null ? _vfxConfig.BeamWidth : 0.15f;
            float width = MiningVFXFormulas.CalculatePulseWidth(baseWidth, pulseSpeed, pulseAmplitude, _beamTime);

            _lineRenderer.startWidth = width;
            _lineRenderer.endWidth = width * 0.7f; // Taper toward asteroid
            _lineRenderer.startColor = beamColor;
            _lineRenderer.endColor = beamColor;

            Vector3 beamOrigin = _miningArmOrigin != null ? _miningArmOrigin.position : transform.position;
            _lineRenderer.SetPosition(0, beamOrigin);
            _lineRenderer.SetPosition(1, targetPos);

            // --- Impact sparks ---
            UpdateSparkSystem(targetPos, beamColor);

            // --- Heat shimmer at mining arm ---
            UpdateShimmerSystem(beamOrigin);
        }

        private void CreateSparkSystem()
        {
            var sparkGO = new GameObject("ImpactSparks");
            sparkGO.transform.SetParent(transform, false);
            _sparkSystem = sparkGO.AddComponent<ParticleSystem>();

            _sparkMain = _sparkSystem.main;
            _sparkMain.loop = true;
            _sparkMain.startLifetime = 0.4f;
            _sparkMain.startSpeed = 3f;
            _sparkMain.startSize = 0.05f;
            _sparkMain.simulationSpace = ParticleSystemSimulationSpace.World;
            _sparkMain.maxParticles = 50;

            _sparkEmission = _sparkSystem.emission;
            _sparkEmission.rateOverTime = 0;

            var shape = _sparkSystem.shape;
            shape.shapeType = ParticleSystemShapeType.Cone;
            shape.angle = 45f;
            shape.radius = 0.1f;

            // Assign spark particle material if available
            var renderer = _sparkSystem.GetComponent<ParticleSystemRenderer>();
            renderer.renderMode = ParticleSystemRenderMode.Billboard;

            _sparkSystem.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
        }

        private void CreateShimmerSystem()
        {
            var shimmerGO = new GameObject("HeatShimmer");
            shimmerGO.transform.SetParent(transform, false);
            _shimmerSystem = shimmerGO.AddComponent<ParticleSystem>();

            var main = _shimmerSystem.main;
            main.loop = true;
            main.startLifetime = 0.8f;
            main.startSpeed = 0.5f;
            main.startSize = 0.3f;
            main.maxParticles = 5;
            main.simulationSpace = ParticleSystemSimulationSpace.World;

            var emission = _shimmerSystem.emission;
            emission.rateOverTime = 3f;

            var shape = _shimmerSystem.shape;
            shape.shapeType = ParticleSystemShapeType.Sphere;
            shape.radius = 0.1f;

            var colorOverLifetime = _shimmerSystem.colorOverLifetime;
            colorOverLifetime.enabled = true;
            var gradient = new Gradient();
            gradient.SetKeys(
                new[] { new GradientColorKey(Color.white, 0f), new GradientColorKey(Color.white, 1f) },
                new[] { new GradientAlphaKey(0.3f, 0f), new GradientAlphaKey(0f, 1f) }
            );
            colorOverLifetime.color = gradient;

            _shimmerSystem.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
        }

        private void UpdateSparkSystem(Vector3 impactPos, Color oreColor)
        {
            if (_sparkSystem == null) return;

            _sparkSystem.transform.position = impactPos;

            int emissionRate = _vfxConfig != null ? _vfxConfig.SparkEmissionRate : 15;
            _sparkEmission.rateOverTime = emissionRate;

            _sparkMain.startLifetime = _vfxConfig != null ? _vfxConfig.SparkLifetime : 0.4f;
            _sparkMain.startSpeed = _vfxConfig != null ? _vfxConfig.SparkSpeed : 3f;
            _sparkMain.startColor = oreColor;

            if (!_sparkSystem.isPlaying)
                _sparkSystem.Play();
        }

        private void UpdateShimmerSystem(Vector3 armPos)
        {
            if (_shimmerSystem == null) return;

            _shimmerSystem.transform.position = armPos;

            float shimmerScale = _vfxConfig != null ? _vfxConfig.HeatHazeScale : 0.3f;
            var main = _shimmerSystem.main;
            main.startSize = shimmerScale;

            if (!_shimmerSystem.isPlaying)
                _shimmerSystem.Play();
        }

        private void StopAllEffects()
        {
            _effectsActive = false;
            _beamTime = 0f;

            if (_sparkSystem != null && _sparkSystem.isPlaying)
                _sparkSystem.Stop(true, ParticleSystemStopBehavior.StopEmitting);

            if (_shimmerSystem != null && _shimmerSystem.isPlaying)
                _shimmerSystem.Stop(true, ParticleSystemStopBehavior.StopEmitting);
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

    /// <summary>
    /// Extension methods for transform hierarchy search.
    /// </summary>
    internal static class TransformExtensions
    {
        public static Transform FindChildRecursive(this Transform parent, string name)
        {
            foreach (Transform child in parent)
            {
                if (child.name == name) return child;
                var found = child.FindChildRecursive(name);
                if (found != null) return found;
            }
            return null;
        }
    }
}
