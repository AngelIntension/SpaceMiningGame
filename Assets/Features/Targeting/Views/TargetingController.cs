using UnityEngine;
using UnityEngine.UIElements;
using Unity.Entities;
using Unity.Transforms;
using VContainer;
using VoidHarvest.Core.EventBus;
using VoidHarvest.Core.Extensions;
using VoidHarvest.Core.State;
using Unity.Cinemachine;
using VoidHarvest.Features.Targeting.Data;
using VoidHarvest.Features.Targeting.Systems;
using VoidHarvest.Features.Mining.Data;
using VoidHarvest.Features.Ship.Data;

namespace VoidHarvest.Features.Targeting.Views
{
    /// <summary>
    /// Orchestrates the targeting overlay: manages ReticleView, OffScreenIndicatorView,
    /// LockProgressView, TargetCardPanelView, caches world positions, handles lock
    /// tick/cancellation, detects target destruction, and suppresses targeting while docked.
    /// See Spec 007: In-Flight Targeting.
    /// </summary>
    public sealed class TargetingController : MonoBehaviour
    {
        [SerializeField] private UIDocument uiDocument;
        [SerializeField] private ShipArchetypeConfig shipConfig;

        private IStateStore _stateStore;
        private IEventBus _eventBus;
        private TargetingConfig _config;
        private TargetingVFXConfig _vfxConfig;
        private Camera _mainCamera;

        private ReticleView _reticleView;
        private OffScreenIndicatorView _offScreenView;
        private LockProgressView _lockProgressView;
        private TargetCardPanelView _cardPanelView;
        private TargetPreviewManager _previewManager;

        private EntityManager _entityManager;
        private bool _ecsReady;
        private Entity _shipEntity;

        private int _cachedTargetId = -1;
        private Vector3 _cachedPosition;
        private float _cachedRadius;
        private bool _cacheValid;

        private Transform _shipTransform;
        private int _prevSelectionId = -1;
        private float[] _cornerOpacities = new float[] { 1f, 1f, 1f, 1f };

        [Inject]
        public void Construct(IStateStore stateStore, IEventBus eventBus,
                              TargetingConfig config, TargetingVFXConfig vfxConfig)
        {
            _stateStore = stateStore;
            _eventBus = eventBus;
            _config = config;
            _vfxConfig = vfxConfig;
        }

        private void Start()
        {
            _mainCamera = Camera.main;
            TryInitializeECS();

            _previewManager = FindObjectOfType<TargetPreviewManager>();

            // Cache the Cinemachine tracking target (the ship's actual transform)
            var cinemachineCam = FindObjectOfType<CinemachineCamera>();
            if (cinemachineCam != null)
                _shipTransform = cinemachineCam.Target.TrackingTarget;

            if (uiDocument != null && _config != null)
            {
                var root = uiDocument.rootVisualElement;
                _reticleView = new ReticleView(root, _config);
                _offScreenView = new OffScreenIndicatorView(root, _config);
                _lockProgressView = new LockProgressView(root, _config,
                    _vfxConfig ?? ScriptableObject.CreateInstance<TargetingVFXConfig>());
                _cardPanelView = new TargetCardPanelView(root, _stateStore, _eventBus, _previewManager);
            }
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

        /// <summary>
        /// Get the world position and visual radius for any target (selected or locked).
        /// Uses cache for selected target, live ECS/GO lookup for others.
        /// </summary>
        public bool GetTargetWorldPosition(int targetId, out Vector3 position, out float radius)
        {
            if (_cacheValid && _cachedTargetId == targetId)
            {
                position = _cachedPosition;
                radius = _cachedRadius;
                return true;
            }

            if (!_ecsReady)
            {
                position = Vector3.zero;
                radius = 0f;
                return false;
            }

            // Live lookup for locked targets
            if (TryGetAsteroidPosition(targetId, out position, out radius))
                return true;
            if (TryGetStationPosition(targetId, out position, out radius))
                return true;

            position = Vector3.zero;
            radius = 0f;
            return false;
        }

        /// <summary>
        /// Attempts to begin lock acquisition on the currently selected target.
        /// Validates capacity, range, and duplicate locks before dispatching.
        /// </summary>
        public void AttemptLockOnSelected()
        {
            if (_stateStore == null || shipConfig == null) return;

            var targeting = _stateStore.Current.Loop.Targeting;
            var selection = targeting.Selection;

            if (!selection.HasSelection) return;

            int targetId = selection.TargetId;

            if (targeting.LockedTargets.Length >= shipConfig.MaxTargetLocks)
            {
                _eventBus?.Publish(new LockSlotsFullEvent());
                return;
            }

            for (int i = 0; i < targeting.LockedTargets.Length; i++)
            {
                if (targeting.LockedTargets[i].TargetId == targetId)
                    return;
            }

            if (targeting.LockAcquisition.IsActive && targeting.LockAcquisition.TargetId == targetId)
                return;

            if (_cacheValid)
            {
                Vector3 shipPos = GetShipPosition();
                float range = Vector3.Distance(shipPos, _cachedPosition);
                if (range > shipConfig.MaxLockRange)
                {
                    var failEvt = new LockFailedEvent(targetId, LockFailReason.OutOfRange);
                    _eventBus?.Publish(in failEvt);
                    return;
                }
            }

            var targetInfo = new TargetInfo(
                selection.TargetId, selection.DisplayName,
                selection.TypeLabel, selection.TargetType);
            float lockTime = LockTimeMath.CalculateLockTime(shipConfig.BaseLockTime, targetInfo);

            _stateStore.Dispatch(new BeginLockAction(targetId, lockTime));
        }

        private void Update()
        {
            if (_stateStore == null) return;

            // Suppress lock tick while docked (FR-035)
            bool isDocked = _stateStore.Current.Loop.Docking.IsDocked;
            if (isDocked) return;

            var targeting = _stateStore.Current.Loop.Targeting;
            var acquisition = targeting.LockAcquisition;

            if (acquisition.IsActive)
            {
                _stateStore.Dispatch(new LockTickAction(Time.deltaTime));

                targeting = _stateStore.Current.Loop.Targeting;
                acquisition = targeting.LockAcquisition;

                if (acquisition.Status == LockAcquisitionStatus.Completed)
                {
                    _stateStore.Dispatch(new CompleteLockAction());
                    var lockEvt = new TargetLockedEvent(
                        acquisition.TargetId,
                        targeting.Selection.DisplayName);
                    _eventBus?.Publish(in lockEvt);
                    _lockProgressView?.TriggerFlash();
                }

                if (acquisition.IsActive && _cacheValid && shipConfig != null)
                {
                    Vector3 shipPos = GetShipPosition();
                    float range = Vector3.Distance(shipPos, _cachedPosition);
                    if (range > shipConfig.MaxLockRange)
                    {
                        _stateStore.Dispatch(new CancelLockAction());
                        var failEvt = new LockFailedEvent(
                            acquisition.TargetId, LockFailReason.OutOfRange);
                        _eventBus?.Publish(in failEvt);
                    }
                }
            }

            // Selection change cancellation
            var currentSelectionId = targeting.Selection.HasSelection
                ? targeting.Selection.TargetId : -1;
            if (_prevSelectionId != currentSelectionId && _prevSelectionId >= 0)
            {
                var currentAcq = _stateStore.Current.Loop.Targeting.LockAcquisition;
                if (currentAcq.IsActive)
                {
                    _stateStore.Dispatch(new CancelLockAction());
                    var failEvt = new LockFailedEvent(
                        currentAcq.TargetId, LockFailReason.Deselected);
                    _eventBus?.Publish(in failEvt);
                }
            }
            _prevSelectionId = currentSelectionId;

            // Locked target destruction detection (T050)
            if (_ecsReady)
                CheckLockedTargetDestruction();
        }

        private void CheckLockedTargetDestruction()
        {
            var lockedTargets = _stateStore.Current.Loop.Targeting.LockedTargets;
            for (int i = lockedTargets.Length - 1; i >= 0; i--)
            {
                var lockData = lockedTargets[i];
                if (lockData.TargetType != TargetType.Asteroid) continue;

                bool exists = TryGetAsteroidPosition(lockData.TargetId, out _, out _);
                if (!exists)
                {
                    _stateStore.Dispatch(new UnlockTargetAction(lockData.TargetId));
                    var lostEvt = new TargetLostEvent(lockData.TargetId);
                    _eventBus?.Publish(in lostEvt);
                }
            }
        }

        private void LateUpdate()
        {
            if (_stateStore == null || _mainCamera == null) return;

            if (!_ecsReady)
            {
                TryInitializeECS();
                if (!_ecsReady) return;
            }

            // Docked-state guard: hide all targeting UI (FR-035)
            bool isDocked = _stateStore.Current.Loop.Docking.IsDocked;
            if (isDocked)
            {
                _reticleView?.Hide();
                _offScreenView?.Hide();
                _lockProgressView?.Hide();
                _cardPanelView?.ClearAll();
                return;
            }

            var targeting = _stateStore.Current.Loop.Targeting;
            var selection = targeting.Selection;

            _cacheValid = false;
            if (selection.HasSelection)
            {
                if (selection.TargetType == TargetType.Asteroid)
                {
                    _cacheValid = TryGetAsteroidPosition(selection.TargetId,
                        out _cachedPosition, out _cachedRadius);
                }
                else if (selection.TargetType == TargetType.Station)
                {
                    _cacheValid = TryGetStationPosition(selection.TargetId,
                        out _cachedPosition, out _cachedRadius);
                }

                _cachedTargetId = selection.TargetId;

                if (!_cacheValid)
                {
                    var acq = targeting.LockAcquisition;
                    if (acq.IsActive)
                    {
                        _stateStore.Dispatch(new CancelLockAction());
                        var failEvt = new LockFailedEvent(
                            acq.TargetId, LockFailReason.TargetDestroyed);
                        _eventBus?.Publish(in failEvt);
                    }

                    _stateStore.Dispatch(new ClearSelectionAction());
                    var lostEvt = new TargetLostEvent(selection.TargetId);
                    _eventBus?.Publish(in lostEvt);
                    _reticleView?.Hide();
                    _offScreenView?.Hide();
                    _lockProgressView?.Hide();
                    return;
                }
            }

            Vector3 shipPos = GetShipPosition();

            _reticleView?.Update(selection, _cachedPosition, _cachedRadius, _mainCamera, shipPos);

            if (_offScreenView != null)
            {
                Vector3 screenPos = selection.HasSelection
                    ? _mainCamera.WorldToScreenPoint(_cachedPosition)
                    : Vector3.zero;
                _offScreenView.Update(
                    selection.HasSelection, screenPos, Screen.width, Screen.height);
            }

            if (_lockProgressView != null)
            {
                var acq = targeting.LockAcquisition;
                float reticleLeft = 0f, reticleTop = 0f, reticleSize = 0f;
                if (_reticleView != null && _reticleView.IsVisible)
                {
                    var bounds = TargetingMath.CalculateScreenBounds(
                        new Unity.Mathematics.float3(
                            _cachedPosition.x, _cachedPosition.y, _cachedPosition.z),
                        _cachedRadius, _mainCamera);
                    float size = Mathf.Max(bounds.width, bounds.height) + _config.ReticlePadding * 2f;
                    size = Mathf.Clamp(size, _config.ReticleMinSize, _config.ReticleMaxSize);
                    float screenHeight = Screen.height;
                    reticleLeft = bounds.center.x - size * 0.5f;
                    reticleTop = screenHeight - bounds.center.y - size * 0.5f;
                    reticleSize = size;
                }

                _lockProgressView.Update(acq, reticleLeft, reticleTop, reticleSize, _cornerOpacities);
            }

            // Target card panel
            _cardPanelView?.Update(targeting.LockedTargets, this, shipPos);

            // Update preview cameras to mirror ship-to-target perspective
            if (_previewManager != null && _shipTransform != null)
            {
                Vector3 shipVisualPos = _shipTransform.position;
                var locked = targeting.LockedTargets;
                for (int i = 0; i < locked.Length; i++)
                {
                    if (GetTargetWorldPosition(locked[i].TargetId, out var targetPos, out _))
                        _previewManager.UpdatePreviewCamera(locked[i].TargetId, shipVisualPos, targetPos);
                }
            }
        }

        private Vector3 GetShipPosition()
        {
            if (_ecsReady && _entityManager.Exists(_shipEntity)
                && _entityManager.HasComponent<LocalTransform>(_shipEntity))
            {
                var shipTransform = _entityManager.GetComponentData<LocalTransform>(_shipEntity);
                return new Vector3(
                    shipTransform.Position.x,
                    shipTransform.Position.y,
                    shipTransform.Position.z);
            }
            return Vector3.zero;
        }

        private bool TryGetAsteroidPosition(int targetId, out Vector3 position, out float radius)
        {
            position = Vector3.zero;
            radius = 0f;

            var query = _entityManager.CreateEntityQuery(
                typeof(AsteroidComponent), typeof(LocalTransform));
            var entities = query.ToEntityArray(Unity.Collections.Allocator.Temp);

            for (int i = 0; i < entities.Length; i++)
            {
                if (entities[i].Index == targetId)
                {
                    var transform = _entityManager.GetComponentData<LocalTransform>(entities[i]);
                    var asteroid = _entityManager.GetComponentData<AsteroidComponent>(entities[i]);
                    position = new Vector3(
                        transform.Position.x, transform.Position.y, transform.Position.z);
                    radius = asteroid.Radius;
                    entities.Dispose();
                    return true;
                }
            }

            entities.Dispose();
            return false;
        }

        private bool TryGetStationPosition(int targetId, out Vector3 position, out float radius)
        {
            position = Vector3.zero;
            radius = 5f;

            var stations = FindObjectsByType<TargetableStation>(FindObjectsSortMode.None);
            foreach (var station in stations)
            {
                if (station.TargetId == targetId)
                {
                    position = station.transform.position;
                    var col = station.GetComponent<Collider>();
                    if (col != null)
                        radius = col.bounds.extents.magnitude;
                    return true;
                }
            }

            return false;
        }
    }
}
