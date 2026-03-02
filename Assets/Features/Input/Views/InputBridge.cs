using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.InputSystem;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using VContainer;
using VoidHarvest.Core.EventBus;
using VoidHarvest.Core.EventBus.Events;
using VoidHarvest.Core.State;
using VoidHarvest.Core.Extensions;
using VoidHarvest.Features.Ship.Data;
using VoidHarvest.Features.Mining.Data;
using VoidHarvest.Features.Mining.Systems;
using VoidHarvest.Features.Docking.Data;

namespace VoidHarvest.Features.Input.Views
{
    /// <summary>
    /// Bridges Unity Input System to ECS PilotCommandComponent and StateStore camera actions.
    /// MonoBehaviour — no game state stored here.
    /// See MVP-01: 6DOF Newtonian flight, MVP-02: Camera orbit and zoom.
    /// </summary>
    public sealed class InputBridge : MonoBehaviour
    {
        [SerializeField] private InputActionAsset inputActions;

        private InputAction _thrustAction;
        private InputAction _strafeAction;
        private InputAction _rollAction;
        private InputAction _orbitAction;
        private InputAction _zoomAction;
        private InputAction _freeLookToggleAction;
        private InputAction _selectAction;
        private InputAction _radialMenuAction;
        private InputAction[] _hotbarActions = new InputAction[8];
        private bool _isMining;

        private UnityEngine.Camera _mainCamera;
        private int _selectedTargetId = -1;
        private float3 _alignPoint;
        private bool _hasAlignPoint;
        private float _lastClickTime;
        private const float DoubleClickWindow = 0.3f;
        private int _radialAction = -1;
        private float _radialDistance;
        private Vector2 _radialMenuStartPos;
        private const float RadialMenuDragThreshold = 5f;

        private IStateStore _stateStore;
        private IEventBus _eventBus;
        private EntityManager _entityManager;
        private Entity _shipEntity;
        private bool _ecsReady;

        private Features.Camera.Views.CameraView _cameraView;
        private Entity _selectedAsteroidEntity;
        private bool _radialMenuOpen;
        private int _uiScrollBlockCount;
        private TargetType _selectedTargetType = TargetType.None;
        private DockingPortComponent _selectedDockingPort;

        /// <summary>Type of the currently selected target (Asteroid, Station, None).</summary>
        public TargetType SelectedTargetType => _selectedTargetType;

        /// <summary>
        /// DI injection point for state store and event bus. See MVP-01: 6DOF Newtonian flight.
        /// </summary>
        [Inject]
        public void Construct(IStateStore stateStore, IEventBus eventBus)
        {
            _stateStore = stateStore;
            _eventBus = eventBus;
        }

        private void Awake()
        {
            if (inputActions == null) return;

            var playerMap = inputActions.FindActionMap("Player");
            var cameraMap = inputActions.FindActionMap("Camera");

            if (playerMap != null)
            {
                _thrustAction = playerMap.FindAction("Thrust");
                _strafeAction = playerMap.FindAction("Strafe");
                _rollAction = playerMap.FindAction("Roll");
                _selectAction = playerMap.FindAction("Select");
                _radialMenuAction = playerMap.FindAction("RadialMenu");

                for (int i = 0; i < 8; i++)
                    _hotbarActions[i] = playerMap.FindAction($"Hotbar{i + 1}");
            }

            if (cameraMap != null)
            {
                _orbitAction = cameraMap.FindAction("Orbit");
                _zoomAction = cameraMap.FindAction("Zoom");
                _freeLookToggleAction = cameraMap.FindAction("FreeLookToggle");
            }
        }

        private void OnEnable()
        {
            inputActions?.Enable();
            if (_freeLookToggleAction != null)
                _freeLookToggleAction.performed += OnFreeLookToggle;
            if (_selectAction != null)
                _selectAction.performed += OnSelect;
            if (_radialMenuAction != null)
            {
                _radialMenuAction.performed += OnRadialMenuStart;
                _radialMenuAction.canceled += OnRadialMenuRelease;
            }

            // Hotbar 1 = mining laser toggle (MVP)
            if (_hotbarActions[0] != null)
                _hotbarActions[0].performed += OnHotbar1;
        }

        private void OnDisable()
        {
            inputActions?.Disable();
            if (_freeLookToggleAction != null)
                _freeLookToggleAction.performed -= OnFreeLookToggle;
            if (_selectAction != null)
                _selectAction.performed -= OnSelect;
            if (_radialMenuAction != null)
            {
                _radialMenuAction.performed -= OnRadialMenuStart;
                _radialMenuAction.canceled -= OnRadialMenuRelease;
            }

            if (_hotbarActions[0] != null)
                _hotbarActions[0].performed -= OnHotbar1;
        }

        private void Start()
        {
            TryInitializeECS();
            _cameraView = FindFirstObjectByType<Features.Camera.Views.CameraView>();

            if (_eventBus != null)
                ListenForUndockingStarted().Forget();
        }

        private async UniTaskVoid ListenForUndockingStarted()
        {
            var ct = this.GetCancellationTokenOnDestroy();
            await foreach (var _ in _eventBus.Subscribe<UndockingStartedEvent>().WithCancellation(ct))
            {
                InitiateUndocking();
            }
        }

        private void Update()
        {
            if (!_ecsReady)
            {
                TryInitializeECS();
                if (!_ecsReady) return;
            }

            UpdatePilotCommand();
            UpdateCameraActions();
        }

        private void TryInitializeECS()
        {
            var world = World.DefaultGameObjectInjectionWorld;
            if (world == null || !world.IsCreated) return;

            _entityManager = world.EntityManager;

            // Find the player ship entity
            var query = _entityManager.CreateEntityQuery(typeof(PlayerControlledTag));
            if (query.CalculateEntityCount() > 0)
            {
                var entities = query.ToEntityArray(Unity.Collections.Allocator.Temp);
                _shipEntity = entities[0];
                entities.Dispose();
                _ecsReady = true;
            }
        }

        private void UpdatePilotCommand()
        {
            float forward = _thrustAction?.ReadValue<float>() ?? 0f;
            float strafe = _strafeAction?.ReadValue<float>() ?? 0f;
            float roll = _rollAction?.ReadValue<float>() ?? 0f;

            // Manual input cancels align-to-point and radial autopilot
            bool hasManualInput = Mathf.Abs(forward) > 0.01f ||
                                  Mathf.Abs(strafe) > 0.01f ||
                                  Mathf.Abs(roll) > 0.01f;
            if (hasManualInput)
            {
                _hasAlignPoint = false;
                _radialAction = -1;
                _radialDistance = 0f;

                // Cancel docking if manual thrust during Docking flight mode
                if (_ecsReady && _entityManager.Exists(_shipEntity)
                    && _entityManager.HasComponent<DockingStateComponent>(_shipEntity))
                {
                    _entityManager.RemoveComponent<DockingStateComponent>(_shipEntity);
                    _stateStore?.Dispatch(new CancelDockingAction());
                    _eventBus?.Publish(new DockingCancelledEvent());
                }
            }

            if (!_entityManager.Exists(_shipEntity)) return;

            _entityManager.SetComponentData(_shipEntity, new PilotCommandComponent
            {
                Forward = forward,
                Strafe = strafe,
                Roll = roll,
                SelectedTargetId = _selectedTargetId,
                AlignPoint = _alignPoint,
                HasAlignPoint = _hasAlignPoint,
                RadialAction = _radialAction,
                RadialDistance = _radialDistance
            });

            // radialAction and radialDistance persist until manual input cancels them
        }

        private void UpdateCameraActions()
        {
            if (_stateStore == null) return;

            // Orbit: only when right mouse button is held (EVE Online style)
            var mouse = Mouse.current;
            bool rightHeld = mouse != null && mouse.rightButton.isPressed;

            var orbitDelta = _orbitAction?.ReadValue<Vector2>() ?? Vector2.zero;
            if (rightHeld && orbitDelta.sqrMagnitude > 0.001f)
            {
                bool freeLookActive = _stateStore.Current.Camera.FreeLookActive;

                if (freeLookActive)
                {
                    _stateStore.Dispatch(new FreeLookAction(orbitDelta.x * 0.1f, orbitDelta.y * 0.1f));
                }
                else
                {
                    _stateStore.Dispatch(new OrbitAction(orbitDelta.x * 0.1f, orbitDelta.y * 0.1f));
                }
            }

            // Zoom: scroll wheel (skip when pointer is over a scrollable UI panel)
            float zoomDelta = _zoomAction?.ReadValue<float>() ?? 0f;
            if (Mathf.Abs(zoomDelta) > 0.001f && _uiScrollBlockCount <= 0)
            {
                _stateStore.Dispatch(new ZoomAction(-zoomDelta * 2f));
                _cameraView?.NotifyManualZoom();
            }
        }

        private void OnFreeLookToggle(InputAction.CallbackContext ctx)
        {
            _stateStore?.Dispatch(new ToggleFreeLookAction());
        }

        /// <summary>
        /// Called by RadialMenuController to prevent clicks on UI from clearing selection.
        /// </summary>
        public void SetRadialMenuOpen(bool open) => _radialMenuOpen = open;

        /// <summary>
        /// Called by UI panels with scroll content to block scroll-wheel zoom
        /// while the pointer is over them. Ref-counted for overlapping panels.
        /// </summary>
        public void SetPointerOverScrollUI(bool over)
        {
            _uiScrollBlockCount += over ? 1 : -1;
            if (_uiScrollBlockCount < 0) _uiScrollBlockCount = 0;
        }

        private void OnSelect(InputAction.CallbackContext ctx)
        {
            if (_radialMenuOpen) return;

            float now = Time.unscaledTime;
            bool isDoubleClick = (now - _lastClickTime) < DoubleClickWindow;
            _lastClickTime = now;

            // Try physics raycast first (for stations on Selectable layer)
            if (TryRaycastSelectable(out var hit))
            {
                if (isDoubleClick)
                {
                    _alignPoint = hit.point;
                    _hasAlignPoint = true;
                }
                else
                {
                    _selectedTargetId = hit.collider.gameObject.GetInstanceID();
                    _selectedAsteroidEntity = Entity.Null;

                    // Check if hit object has a DockingPortComponent (station)
                    var dockingPort = hit.collider.GetComponentInChildren<DockingPortComponent>();
                    if (dockingPort != null)
                    {
                        _selectedTargetType = TargetType.Station;
                        _selectedDockingPort = dockingPort;
                    }
                    else
                    {
                        _selectedTargetType = TargetType.None;
                        _selectedDockingPort = null;
                    }

                    _eventBus?.Publish(new TargetSelectedEvent(_selectedTargetId));
                }
                return;
            }

            // Fallback: ECS ray-sphere test against asteroids
            if (TryRaycastAsteroid(out var hitPoint, out var hitEntity))
            {
                if (isDoubleClick)
                {
                    _alignPoint = hitPoint;
                    _hasAlignPoint = true;
                }
                else
                {
                    _selectedTargetId = hitEntity.Index;
                    _selectedAsteroidEntity = hitEntity;
                    _selectedTargetType = TargetType.Asteroid;
                    _selectedDockingPort = null;
                    _eventBus?.Publish(new TargetSelectedEvent(_selectedTargetId));
                }
                return;
            }

            // No hit — clear selection
            _selectedTargetId = -1;
            _selectedAsteroidEntity = Entity.Null;
            _selectedTargetType = TargetType.None;
            _selectedDockingPort = null;
            _eventBus?.Publish(new TargetSelectedEvent(-1));
        }

        private void OnRadialMenuStart(InputAction.CallbackContext ctx)
        {
            var mouse = Mouse.current;
            _radialMenuStartPos = mouse != null ? mouse.position.ReadValue() : Vector2.zero;
        }

        private void OnRadialMenuRelease(InputAction.CallbackContext ctx)
        {
            if (_selectedTargetType == TargetType.None) return;

            // Only open radial menu if right-click was a tap (not a drag for orbit)
            var mouse = Mouse.current;
            if (mouse == null) return;
            Vector2 endPos = mouse.position.ReadValue();
            if (Vector2.Distance(_radialMenuStartPos, endPos) > RadialMenuDragThreshold) return;

            _eventBus?.Publish(new RadialMenuRequestedEvent(_selectedTargetId, _selectedTargetType));
        }

        /// <summary>
        /// Called by RadialMenuController to set the chosen radial action and distance.
        /// These are one-shot values: consumed in the next UpdatePilotCommand then reset.
        /// </summary>
        public void SetRadialChoice(int action, float distance)
        {
            _radialAction = action;
            _radialDistance = distance;
        }

        /// <summary>
        /// Hotbar 1: Mining laser toggle. Activates/deactivates mining on selected target.
        /// Dual-write: dispatches to StateStore (MiningReducer) AND writes MiningBeamComponent to ECS.
        /// See MVP-05: Mining beam and yield.
        /// </summary>
        private void OnHotbar1(InputAction.CallbackContext ctx)
        {
            if (!_ecsReady || _stateStore == null) return;

            if (_isMining)
            {
                StopMining();
            }
            else if (_selectedTargetId >= 0)
            {
                StartMining();
            }
        }

        private void StartMining()
        {
            if (!_entityManager.Exists(_shipEntity)) return;
            if (_selectedAsteroidEntity == Entity.Null || !_entityManager.Exists(_selectedAsteroidEntity)) return;

            // Resolve ore ID from the selected asteroid entity
            string oreId = "unknown";
            if (_entityManager.HasComponent<AsteroidOreComponent>(_selectedAsteroidEntity))
            {
                int oreTypeId = _entityManager.GetComponentData<AsteroidOreComponent>(_selectedAsteroidEntity).OreTypeId;
                oreId = OreTypeBlobBakingSystem.GetOreId(oreTypeId);
                if (string.IsNullOrEmpty(oreId)) oreId = "unknown";
            }

            // Dispatch BeginMiningAction to StateStore
            _stateStore.Dispatch(new BeginMiningAction(_selectedTargetId, oreId));

            // Publish MiningStartedEvent
            _eventBus?.Publish(new MiningStartedEvent(_selectedTargetId, oreId));

            // Add MiningBeamComponent on ship entity via EntityManager (added at runtime, not baked)
            var config = _entityManager.GetComponentData<ShipConfigComponent>(_shipEntity);
            var beam = new MiningBeamComponent
            {
                TargetAsteroid = _selectedAsteroidEntity,
                BeamEnergy = 1f,
                MiningPower = config.MiningPower,
                MaxRange = 50f,
                Active = true
            };
            if (_entityManager.HasComponent<MiningBeamComponent>(_shipEntity))
                _entityManager.SetComponentData(_shipEntity, beam);
            else
                _entityManager.AddComponentData(_shipEntity, beam);

            _isMining = true;

        }

        private void StopMining()
        {
            if (!_entityManager.Exists(_shipEntity)) return;

            // Dispatch StopMiningAction to StateStore
            _stateStore.Dispatch(new StopMiningAction());

            // Deactivate MiningBeamComponent
            if (!_entityManager.HasComponent<MiningBeamComponent>(_shipEntity)) return;
            var beam = _entityManager.GetComponentData<MiningBeamComponent>(_shipEntity);
            var targetIndex = beam.TargetAsteroid.Index;
            beam.Active = false;
            _entityManager.SetComponentData(_shipEntity, beam);

            _isMining = false;

            var evt = new MiningStoppedEvent(targetIndex, StopReason.PlayerStopped);
            _eventBus?.Publish(in evt);
        }

        /// <summary>
        /// Called by RadialMenuController to start mining via Mine action.
        /// </summary>
        public void StartMiningFromRadial()
        {
            if (_selectedTargetId >= 0 && !_isMining)
                StartMining();
        }

        /// <summary>
        /// Returns the DockingPortComponent of the currently selected station, if any.
        /// </summary>
        public DockingPortComponent GetSelectedDockingPort() => _selectedDockingPort;

        /// <summary>
        /// Called by RadialMenuController to initiate docking at the ECS level.
        /// Adds DockingStateComponent to the ship entity with target port data.
        /// </summary>
        public void InitiateDocking(DockingPortComponent port)
        {
            if (!_ecsReady || !_entityManager.Exists(_shipEntity) || port == null) return;

            var dockingState = new DockingStateComponent
            {
                Phase = DockingPhase.Approaching,
                TargetPortPosition = port.WorldPortPosition,
                TargetPortRotation = port.WorldPortRotation,
                TargetStationId = port.StationId,
                SnapTimer = 0f
            };

            if (_entityManager.HasComponent<DockingStateComponent>(_shipEntity))
                _entityManager.SetComponentData(_shipEntity, dockingState);
            else
                _entityManager.AddComponentData(_shipEntity, dockingState);

            // Set align point toward port for approach
            _alignPoint = port.WorldPortPosition;
            _hasAlignPoint = true;
        }

        /// <summary>
        /// Called to initiate undocking at the ECS level.
        /// Transitions DockingStateComponent to Undocking phase.
        /// </summary>
        public void InitiateUndocking()
        {
            if (!_ecsReady || !_entityManager.Exists(_shipEntity)) return;
            if (!_entityManager.HasComponent<DockingStateComponent>(_shipEntity)) return;

            var docking = _entityManager.GetComponentData<DockingStateComponent>(_shipEntity);
            docking.Phase = DockingPhase.Undocking;
            docking.SnapTimer = 0f;
            _entityManager.SetComponentData(_shipEntity, docking);
        }

        /// <summary>
        /// Called by RadialMenuController to auto-fly toward the selected target.
        /// Sets align point + radial action so the ECS autopilot handles thrust and rotation.
        /// </summary>
        public void ApproachSelectedTarget(float stopDistance = 50f)
        {
            if (!_ecsReady) return;
            if (_selectedAsteroidEntity == Entity.Null || !_entityManager.Exists(_selectedAsteroidEntity)) return;
            var transform = _entityManager.GetComponentData<LocalTransform>(_selectedAsteroidEntity);
            _alignPoint = transform.Position;
            _hasAlignPoint = true;
        }

        private bool TryRaycastSelectable(out RaycastHit hit)
        {
            hit = default;

            if (_mainCamera == null)
                _mainCamera = UnityEngine.Camera.main;
            if (_mainCamera == null) return false;

            var mouse = Mouse.current;
            if (mouse == null) return false;

            Vector2 mousePos = mouse.position.ReadValue();
            Ray ray = _mainCamera.ScreenPointToRay(new Vector3(mousePos.x, mousePos.y, 0f));
            int selectableLayer = LayerMask.GetMask("Selectable");

            return Physics.Raycast(ray, out hit, Mathf.Infinity, selectableLayer);
        }

        private bool TryRaycastAsteroid(out float3 hitPoint, out Entity hitEntity)
        {
            hitPoint = float3.zero;
            hitEntity = Entity.Null;

            if (!_ecsReady) return false;

            if (_mainCamera == null)
                _mainCamera = UnityEngine.Camera.main;
            if (_mainCamera == null) return false;

            var mouse = Mouse.current;
            if (mouse == null) return false;

            Vector2 mousePos = mouse.position.ReadValue();
            Ray ray = _mainCamera.ScreenPointToRay(new Vector3(mousePos.x, mousePos.y, 0f));
            float3 rayOrigin = ray.origin;
            float3 rayDir = math.normalize((float3)ray.direction);

            float closestDist = float.MaxValue;

            var query = _entityManager.CreateEntityQuery(
                typeof(AsteroidComponent), typeof(LocalTransform));
            var entities = query.ToEntityArray(Unity.Collections.Allocator.Temp);

            for (int i = 0; i < entities.Length; i++)
            {
                var entity = entities[i];
                var transform = _entityManager.GetComponentData<LocalTransform>(entity);
                var asteroid = _entityManager.GetComponentData<AsteroidComponent>(entity);

                float3 center = transform.Position;
                float radius = asteroid.Radius;

                // Ray-sphere intersection
                float3 oc = rayOrigin - center;
                float b = math.dot(oc, rayDir);
                float c = math.dot(oc, oc) - radius * radius;
                float discriminant = b * b - c;

                if (discriminant > 0)
                {
                    float t = -b - math.sqrt(discriminant);
                    if (t > 0 && t < closestDist)
                    {
                        closestDist = t;
                        hitPoint = rayOrigin + rayDir * t;
                        hitEntity = entity;
                    }
                }
            }

            entities.Dispose();
            return hitEntity != Entity.Null;
        }
    }
}
