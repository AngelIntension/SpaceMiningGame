using Cysharp.Threading.Tasks;
using UnityEngine;
using VContainer;
using VoidHarvest.Core.EventBus;
using VoidHarvest.Core.EventBus.Events;
using VoidHarvest.Features.Mining.Data;

namespace VoidHarvest.Features.Mining.Views
{
    /// <summary>
    /// Subscribes to ThresholdCrossedEvent and spawns crumble burst VFX at asteroid position.
    /// Escalating particle count per threshold tier. Fragment explosion on final threshold.
    /// // CONSTITUTION DEVIATION: Mutable view-layer side effects (ParticleSystem),
    /// // isolated to rendering boundary.
    /// See FR-009: Crumble bursts, FR-011: Fragment explosion.
    /// </summary>
    public sealed class DepletionVFXView : MonoBehaviour
    {
        private IEventBus _eventBus;
        private DepletionVFXConfig _config;

        // Burst particle system for crumble effects
        private ParticleSystem _crumbleSystem;
        // Fragment explosion system
        private ParticleSystem _fragmentSystem;
        // Flash quad
        private ParticleSystem _flashSystem;

        // Cached runtime particle material (additive billboard)
        private static Material _particleMaterial;

        [Inject]
        public void Construct(IEventBus eventBus, DepletionVFXConfig config)
        {
            _eventBus = eventBus;
            _config = config;
        }

        private void Start()
        {
            CreateCrumbleSystem();
            CreateFragmentSystem();
            CreateFlashSystem();

            if (_eventBus != null)
                SubscribeToThresholdEvents().Forget();
        }

        private async UniTaskVoid SubscribeToThresholdEvents()
        {
            var cts = this.GetCancellationTokenOnDestroy();
            await foreach (var evt in _eventBus.Subscribe<ThresholdCrossedEvent>().WithCancellation(cts))
            {
                OnThresholdCrossed(evt);
            }
        }

        private void OnThresholdCrossed(ThresholdCrossedEvent evt)
        {
            Vector3 pos = new Vector3(evt.Position.x, evt.Position.y, evt.Position.z);

            if (evt.ThresholdIndex < 3)
            {
                // Crumble burst — escalating count per tier
                SpawnCrumbleBurst(pos, evt.ThresholdIndex, evt.AsteroidRadius);
            }
            else
            {
                // Final threshold — fragment explosion
                SpawnFragmentExplosion(pos, evt.AsteroidRadius);
            }

            // Flash at all thresholds
            SpawnFlash(pos, evt.AsteroidRadius);
        }

        private void SpawnCrumbleBurst(Vector3 position, byte thresholdIndex, float radius)
        {
            if (_crumbleSystem == null || _config == null) return;

            _crumbleSystem.transform.position = position;

            // Escalating count: base * scale^index
            int count = Mathf.RoundToInt(
                _config.CrumbleBurstCountBase * Mathf.Pow(_config.CrumbleBurstCountScale, thresholdIndex));

            var burstParams = new ParticleSystem.EmitParams
            {
                startLifetime = _config.CrumbleBurstLifetime,
                startSize = radius * 0.1f
            };

            _crumbleSystem.Emit(burstParams, count);
        }

        private void SpawnFragmentExplosion(Vector3 position, float radius)
        {
            if (_fragmentSystem == null || _config == null) return;

            _fragmentSystem.transform.position = position;

            var burstParams = new ParticleSystem.EmitParams
            {
                startLifetime = _config.FragmentLifetime,
                startSize = Random.Range(_config.FragmentScaleRange.x, _config.FragmentScaleRange.y) * radius
            };

            _fragmentSystem.Emit(burstParams, _config.FragmentCount);
        }

        private void SpawnFlash(Vector3 position, float radius)
        {
            if (_flashSystem == null || _config == null) return;

            _flashSystem.transform.position = position;

            var burstParams = new ParticleSystem.EmitParams
            {
                startLifetime = _config.CrumbleFlashDuration,
                startSize = radius * 2f
            };

            _flashSystem.Emit(burstParams, 1);
        }

        private static Material GetOrCreateParticleMaterial()
        {
            if (_particleMaterial != null) return _particleMaterial;

            var shader = Shader.Find("Universal Render Pipeline/Particles/Unlit");
            if (shader == null)
                shader = Shader.Find("Particles/Standard Unlit");

            _particleMaterial = new Material(shader);
            _particleMaterial.SetFloat("_Surface", 1f); // Transparent
            _particleMaterial.SetFloat("_SrcBlend", (float)UnityEngine.Rendering.BlendMode.SrcAlpha);
            _particleMaterial.SetFloat("_DstBlend", (float)UnityEngine.Rendering.BlendMode.One);
            _particleMaterial.SetFloat("_ZWrite", 0f);
            _particleMaterial.renderQueue = 3000;
            _particleMaterial.enableInstancing = true;
            _particleMaterial.SetColor("_BaseColor", Color.white);
            _particleMaterial.SetTexture("_BaseMap", ProceduralParticleTextures.SoftCircle);
            return _particleMaterial;
        }

        private void CreateCrumbleSystem()
        {
            var go = new GameObject("CrumbleBurst");
            go.transform.SetParent(transform, false);
            _crumbleSystem = go.AddComponent<ParticleSystem>();

            var main = _crumbleSystem.main;
            main.loop = false;
            main.playOnAwake = false;
            main.startLifetime = 0.5f;
            main.startSpeed = _config != null ? _config.CrumbleBurstSpeed : 5f;
            main.startSize = 0.1f;
            main.startColor = new Color(0.6f, 0.5f, 0.4f, 1f);
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            main.maxParticles = 100;

            var emission = _crumbleSystem.emission;
            emission.rateOverTime = 0;

            var shape = _crumbleSystem.shape;
            shape.shapeType = ParticleSystemShapeType.Sphere;
            shape.radius = 0.5f;

            var crumbleRenderer = _crumbleSystem.GetComponent<ParticleSystemRenderer>();
            crumbleRenderer.renderMode = ParticleSystemRenderMode.Billboard;
            crumbleRenderer.material = GetOrCreateParticleMaterial();

            var sizeOverLifetime = _crumbleSystem.sizeOverLifetime;
            sizeOverLifetime.enabled = true;
            sizeOverLifetime.size = new ParticleSystem.MinMaxCurve(1f,
                new AnimationCurve(new Keyframe(0, 1), new Keyframe(1, 0)));

            _crumbleSystem.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
        }

        private void CreateFragmentSystem()
        {
            var go = new GameObject("FragmentExplosion");
            go.transform.SetParent(transform, false);
            _fragmentSystem = go.AddComponent<ParticleSystem>();

            var main = _fragmentSystem.main;
            main.loop = false;
            main.playOnAwake = false;
            main.startLifetime = _config != null ? _config.FragmentLifetime : 3f;
            main.startSpeed = _config != null ? _config.FragmentSpeed : 4f;
            main.startSize = 0.15f;
            main.startColor = new Color(0.5f, 0.4f, 0.3f, 1f);
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            main.maxParticles = 20;
            main.gravityModifier = 0f;

            var emission = _fragmentSystem.emission;
            emission.rateOverTime = 0;

            var shape = _fragmentSystem.shape;
            shape.shapeType = ParticleSystemShapeType.Sphere;
            shape.radius = 0.3f;

            var fragmentRenderer = _fragmentSystem.GetComponent<ParticleSystemRenderer>();
            fragmentRenderer.renderMode = ParticleSystemRenderMode.Billboard;
            fragmentRenderer.material = GetOrCreateParticleMaterial();

            var colorOverLifetime = _fragmentSystem.colorOverLifetime;
            colorOverLifetime.enabled = true;
            var gradient = new Gradient();
            gradient.SetKeys(
                new[] { new GradientColorKey(Color.white, 0f), new GradientColorKey(new Color(0.5f, 0.4f, 0.3f), 1f) },
                new[] { new GradientAlphaKey(1f, 0f), new GradientAlphaKey(0f, 1f) }
            );
            colorOverLifetime.color = gradient;

            _fragmentSystem.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
        }

        private void CreateFlashSystem()
        {
            var go = new GameObject("CrumbleFlash");
            go.transform.SetParent(transform, false);
            _flashSystem = go.AddComponent<ParticleSystem>();

            var main = _flashSystem.main;
            main.loop = false;
            main.playOnAwake = false;
            main.startLifetime = _config != null ? _config.CrumbleFlashDuration : 0.3f;
            main.startSpeed = 0f;
            main.startSize = 1f;
            main.startColor = new Color(1f, 0.9f, 0.7f, 0.8f);
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            main.maxParticles = 5;

            var emission = _flashSystem.emission;
            emission.rateOverTime = 0;

            var shape = _flashSystem.shape;
            shape.enabled = false;

            var renderer = _flashSystem.GetComponent<ParticleSystemRenderer>();
            renderer.renderMode = ParticleSystemRenderMode.Billboard;
            renderer.material = GetOrCreateParticleMaterial();

            var colorOverLifetime = _flashSystem.colorOverLifetime;
            colorOverLifetime.enabled = true;
            var gradient = new Gradient();
            gradient.SetKeys(
                new[] { new GradientColorKey(Color.white, 0f), new GradientColorKey(Color.white, 1f) },
                new[] { new GradientAlphaKey(0.8f, 0f), new GradientAlphaKey(0f, 1f) }
            );
            colorOverLifetime.color = gradient;

            _flashSystem.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
        }
    }
}
