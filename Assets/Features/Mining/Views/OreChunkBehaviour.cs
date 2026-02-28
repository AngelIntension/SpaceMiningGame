using UnityEngine;
using UnityEngine.Pool;
using VoidHarvest.Core.EventBus;
using VoidHarvest.Core.EventBus.Events;
using VoidHarvest.Features.Mining.Data;
using VoidHarvest.Features.Mining.Systems;

namespace VoidHarvest.Features.Mining.Views
{
    /// <summary>
    /// Per-chunk lifecycle: drift outward, bezier attract to barge, collect on arrival.
    /// Pooled via ObjectPool — zero GC allocations.
    /// // CONSTITUTION DEVIATION: Mutable view-layer fields for position interpolation
    /// // (cosmetic only, not game state).
    /// See FR-016: Bezier attraction, FR-017: Collection flash.
    /// </summary>
    public sealed class OreChunkBehaviour : MonoBehaviour
    {
        private enum Phase { Drift, Attract, Collect }

        // Pre-allocated state — no allocations during lifecycle
        private Phase _phase;
        private float _elapsed;
        private float _totalLifetime;
        private Vector3 _driftDirection;
        private Vector3 _startPos;
        private Vector3 _controlPoint;
        private Transform _targetCollector;
        private float _attractT;
        private float _attractDuration;
        private string _oreId;

        // Config cache
        private OreChunkConfig _config;
        private IEventBus _eventBus;
        private IObjectPool<OreChunkBehaviour> _pool;

        // Material property block for emission (avoids material instance creation)
        private MeshRenderer _renderer;
        private static readonly int EmissionColorId = Shader.PropertyToID("_EmissionColor");
        private MaterialPropertyBlock _propBlock;

        public void Initialize(
            Vector3 spawnPosition,
            Vector3 driftDirection,
            Transform collectorTarget,
            OreChunkConfig config,
            IEventBus eventBus,
            IObjectPool<OreChunkBehaviour> pool,
            Color glowColor,
            float glowIntensity,
            string oreId)
        {
            _config = config;
            _eventBus = eventBus;
            _pool = pool;
            _oreId = oreId;

            transform.position = spawnPosition;
            _startPos = spawnPosition;
            _driftDirection = driftDirection.normalized;
            _targetCollector = collectorTarget;

            _phase = Phase.Drift;
            _elapsed = 0f;
            _totalLifetime = 0f;
            _attractT = 0f;

            // Calculate bezier control point — perpendicular offset for organic curve
            float attractDist = collectorTarget != null
                ? Vector3.Distance(spawnPosition, collectorTarget.position)
                : 10f;
            _attractDuration = attractDist / Mathf.Max(config.AttractionSpeed, 0.1f);

            // Set emission glow
            if (_renderer == null) _renderer = GetComponent<MeshRenderer>();
            if (_renderer != null)
            {
                if (_propBlock == null) _propBlock = new MaterialPropertyBlock();
                _propBlock.SetColor(EmissionColorId, glowColor * glowIntensity);
                _renderer.SetPropertyBlock(_propBlock);
            }

            gameObject.SetActive(true);
        }

        private void Update()
        {
            float dt = Time.deltaTime;
            _elapsed += dt;
            _totalLifetime += dt;

            // Safety: force despawn
            if (_totalLifetime >= (_config != null ? _config.MaxLifetime : 5f))
            {
                ReturnToPool();
                return;
            }

            switch (_phase)
            {
                case Phase.Drift:
                    UpdateDrift(dt);
                    break;
                case Phase.Attract:
                    UpdateAttract(dt);
                    break;
                case Phase.Collect:
                    ReturnToPool();
                    break;
            }
        }

        private void UpdateDrift(float dt)
        {
            float driftDuration = _config != null ? _config.InitialDriftDuration : 0.75f;
            float driftSpeed = _config != null ? _config.InitialDriftSpeed : 2f;

            transform.position += _driftDirection * driftSpeed * dt;

            if (_elapsed >= driftDuration)
            {
                // Transition to attract phase
                _phase = Phase.Attract;
                _elapsed = 0f;
                _startPos = transform.position;

                // Compute bezier control point
                if (_targetCollector != null)
                {
                    Vector3 toTarget = _targetCollector.position - _startPos;
                    Vector3 perp = Vector3.Cross(toTarget.normalized, Vector3.up);
                    if (perp.sqrMagnitude < 0.01f)
                        perp = Vector3.Cross(toTarget.normalized, Vector3.right);
                    perp.Normalize();
                    float offset = toTarget.magnitude * 0.3f;
                    _controlPoint = (_startPos + _targetCollector.position) * 0.5f + perp * offset;
                }
            }
        }

        private void UpdateAttract(float dt)
        {
            if (_targetCollector == null)
            {
                ReturnToPool();
                return;
            }

            float duration = _attractDuration > 0f ? _attractDuration : 1f;
            _attractT += dt / duration;

            if (_attractT >= 1f)
                _attractT = 1f;

            Vector3 target = _targetCollector.position;
            transform.position = MiningVFXFormulas.EvaluateBezier(_startPos, _controlPoint, target, _attractT);

            // Check collection distance
            float dist = Vector3.Distance(transform.position, target);
            if (dist < 0.3f || _attractT >= 1f)
            {
                // Publish collection event
                var pos = new Unity.Mathematics.float3(target.x, target.y, target.z);
                var evt = new OreChunkCollectedEvent(pos, _oreId ?? "");
                _eventBus?.Publish(in evt);

                _phase = Phase.Collect;
            }
        }

        private void ReturnToPool()
        {
            gameObject.SetActive(false);
            _pool?.Release(this);
        }
    }
}
