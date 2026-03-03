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
        [SerializeField] private OreDefinition[] oreDefinitions;

        private IStateStore _stateStore;
        private MiningVFXConfig _vfxConfig;
        private LineRenderer _lineRenderer;

        // Spark particle system at impact point
        private ParticleSystem _sparkSystem;
        private ParticleSystem.EmissionModule _sparkEmission;
        private ParticleSystem.MainModule _sparkMain;

        // Heat shimmer at mining arm
        private ParticleSystem _shimmerSystem;

        // Cached runtime particle material (additive billboard)
        private static Material _particleMaterial;

        // Mining arm origin transform (child of ship)
        private Transform _miningArmOrigin;

        // MeshCollider proxy for exact beam-to-mesh surface raycasting
        private GameObject _meshColliderProxy;
        private MeshCollider _proxyCollider;
        private Entity _currentProxyTarget;

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
            CreateMeshColliderProxy();
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
            if (!_ecsReady || !TryGetTargetPosition(out var asteroidCenter, out var asteroidRadius,
                    out var targetEntity))
            {
                if (_effectsActive)
                    StopAllEffects();
                _lineRenderer.enabled = false;
                return;
            }

            UpdateProxyForTarget(targetEntity);

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

            // Compute surface impact point via mesh raycast for exact hit on irregular geometry
            Vector3 impactPoint = CalculateImpactPoint(beamOrigin, asteroidCenter, asteroidRadius);

            _lineRenderer.SetPosition(0, beamOrigin);
            _lineRenderer.SetPosition(1, impactPoint);

            // --- Impact sparks ---
            UpdateSparkSystem(impactPoint, beamColor);

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

            var renderer = _sparkSystem.GetComponent<ParticleSystemRenderer>();
            renderer.renderMode = ParticleSystemRenderMode.Billboard;
            renderer.material = GetOrCreateParticleMaterial();

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

            var shimmerRenderer = _shimmerSystem.GetComponent<ParticleSystemRenderer>();
            shimmerRenderer.renderMode = ParticleSystemRenderMode.Billboard;
            shimmerRenderer.material = GetOrCreateParticleMaterial();

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

        // Small overshoot into the mesh to guarantee visual beam contact
        private const float ImpactOvershoot = 0.05f;

        private void CreateMeshColliderProxy()
        {
            _meshColliderProxy = new GameObject("MiningBeamImpactProxy");
            _meshColliderProxy.layer = LayerMask.NameToLayer("Ignore Raycast");
            _proxyCollider = _meshColliderProxy.AddComponent<MeshCollider>();
        }

        private void UpdateProxyForTarget(Entity targetEntity)
        {
            // Update mesh only when target changes (mesh lookup + collider bake is expensive)
            if (targetEntity != _currentProxyTarget || _proxyCollider.sharedMesh == null)
            {
                _currentProxyTarget = targetEntity;

                if (!_entityManager.HasComponent<AsteroidComponent>(targetEntity))
                    return;

                var asteroid = _entityManager.GetComponentData<AsteroidComponent>(targetEntity);
                var mesh = AsteroidMeshRegistry.GetMesh(asteroid.MeshIndex);
                if (mesh == null)
                    return;

                _proxyCollider.sharedMesh = mesh;
            }

            // Always sync transform — scale changes continuously during depletion
            var lt = _entityManager.GetComponentData<LocalTransform>(targetEntity);
            _meshColliderProxy.transform.SetPositionAndRotation(
                new Vector3(lt.Position.x, lt.Position.y, lt.Position.z),
                lt.Rotation);
            _meshColliderProxy.transform.localScale = Vector3.one * lt.Scale;
        }

        private Vector3 CalculateImpactPoint(Vector3 beamOrigin, Vector3 asteroidCenter, float asteroidRadius)
        {
            Vector3 toCenter = asteroidCenter - beamOrigin;
            float distance = toCenter.magnitude;

            if (distance < 0.001f)
                return asteroidCenter;

            Vector3 direction = toCenter / distance;

            // Raycast against the actual mesh geometry for exact surface hit
            if (_proxyCollider != null && _proxyCollider.sharedMesh != null)
            {
                var ray = new Ray(beamOrigin, direction);
                if (_proxyCollider.Raycast(ray, out var hit, distance + asteroidRadius))
                    return hit.point + direction * ImpactOvershoot;
            }

            // Fallback: sphere projection
            return asteroidCenter - direction * asteroidRadius;
        }

        private void OnDestroy()
        {
            if (_meshColliderProxy != null)
                Destroy(_meshColliderProxy);
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

        private bool TryGetTargetPosition(out Vector3 center, out float radius, out Entity targetEntity)
        {
            center = Vector3.zero;
            radius = 1f;
            targetEntity = Entity.Null;

            if (!_entityManager.HasComponent<MiningBeamComponent>(_shipEntity))
                return false;

            var beam = _entityManager.GetComponentData<MiningBeamComponent>(_shipEntity);
            if (!beam.Active || beam.TargetAsteroid == Entity.Null)
                return false;

            if (!_entityManager.Exists(beam.TargetAsteroid) ||
                !_entityManager.HasComponent<LocalTransform>(beam.TargetAsteroid))
                return false;

            targetEntity = beam.TargetAsteroid;

            var targetTransform = _entityManager.GetComponentData<LocalTransform>(beam.TargetAsteroid);
            center = targetTransform.Position;

            if (_entityManager.HasComponent<AsteroidComponent>(beam.TargetAsteroid))
            {
                var asteroid = _entityManager.GetComponentData<AsteroidComponent>(beam.TargetAsteroid);
                radius = asteroid.Radius;
            }

            return true;
        }

        private static Material GetOrCreateParticleMaterial()
        {
            if (_particleMaterial != null) return _particleMaterial;

            var shader = Shader.Find("Universal Render Pipeline/Particles/Unlit");
            if (shader == null)
                shader = Shader.Find("Particles/Standard Unlit");

            _particleMaterial = new Material(shader);
            _particleMaterial.SetFloat("_Surface", 1f); // Transparent
            _particleMaterial.SetFloat("_Blend", 0f); // Additive (BlendMode.Additive = 0 for Particles)
            _particleMaterial.SetFloat("_SrcBlend", (float)UnityEngine.Rendering.BlendMode.SrcAlpha);
            _particleMaterial.SetFloat("_DstBlend", (float)UnityEngine.Rendering.BlendMode.One);
            _particleMaterial.SetFloat("_ZWrite", 0f);
            _particleMaterial.renderQueue = 3000; // Transparent queue
            _particleMaterial.enableInstancing = true;
            _particleMaterial.SetColor("_BaseColor", Color.white);
            _particleMaterial.SetTexture("_BaseMap", ProceduralParticleTextures.SoftCircle);
            return _particleMaterial;
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
