using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UIElements;
using VContainer;
using VoidHarvest.Core.EventBus;
using VoidHarvest.Core.EventBus.Events;
using VoidHarvest.Core.State;
using VoidHarvest.Core.Extensions;
using VoidHarvest.Features.Docking.Data;
using VoidHarvest.Features.Input.Views;
using VoidHarvest.Features.Targeting.Views;

namespace VoidHarvest.Features.HUD.Views
{
    /// <summary>
    /// Controls the radial context menu UI. View layer only — no game state.
    /// See MVP-04: Right-click radial menu.
    /// </summary>
    public sealed class RadialMenuController : MonoBehaviour
    {
        private const int ActionApproach = 0;
        private const int ActionOrbit = 1;
        private const int ActionMine = 2;
        private const int ActionKeepAtRange = 3;
        private const int ActionDock = 4;
        private const int ActionLockTarget = 5;

        private const float DefaultApproachDistance = 50f;
        private const float DefaultOrbitDistance = 100f;
        private const float DefaultKeepAtRangeDistance = 50f;

        [SerializeField] private UIDocument uiDocument;

        private IStateStore _stateStore;
        private IEventBus _eventBus;
        private CancellationTokenSource _eventCts;
        private VisualElement _root;
        private VisualElement _distanceSubmenu;
        private SliderInt _distanceSlider;
        private Label _distanceTitle;
        private Button _segmentApproach;
        private Button _segmentOrbit;
        private Button _segmentMine;
        private Button _segmentKeepAtRange;
        private Button _segmentDock;
        private Button _segmentLockTarget;
        private Button _confirmButton;
        private Button _preset25;
        private Button _preset50;
        private Button _preset100;
        private Button _preset250;
        private Button _preset500;

        private TargetType _currentTargetType;

        private int _selectedAction = -1;
        private bool _isOpen;

        // Persisted distance defaults per action (updated when user confirms)
        private float _approachDistance = DefaultApproachDistance;
        private float _orbitDistance = DefaultOrbitDistance;
        private float _keepAtRangeDistance = DefaultKeepAtRangeDistance;

        // Reference to InputBridge for SetRadialChoice callback
        private InputBridge _inputBridge;
        private TargetingController _targetingController;

        /// <summary>
        /// DI injection point for the state store. See MVP-04: Right-click radial menu.
        /// </summary>
        [Inject]
        public void Construct(IStateStore stateStore, IEventBus eventBus,
                              InputBridge inputBridge, TargetingController targetingController)
        {
            _stateStore = stateStore;
            _eventBus = eventBus;
            _inputBridge = inputBridge;
            _targetingController = targetingController;
        }

        private async UniTaskVoid ListenForRadialMenuEvents(CancellationToken ct)
        {
            await foreach (var evt in _eventBus.Subscribe<RadialMenuRequestedEvent>().WithCancellation(ct))
            {
                _currentTargetType = evt.TargetType;
                Open();
            }
        }

        private void OnEnable()
        {
            // Start async EventBus subscription each time we're enabled
            if (_eventBus != null)
            {
                _eventCts = new CancellationTokenSource();
                ListenForRadialMenuEvents(_eventCts.Token).Forget();
            }

            if (uiDocument == null) return;

            var rootVisual = uiDocument.rootVisualElement;
            _root = rootVisual.Q<VisualElement>("radial-menu-root");
            if (_root == null)
            {
                Debug.LogError("[RadialMenuController] Could not find 'radial-menu-root' in UXML.");
                return;
            }

            // Query segment buttons
            _segmentApproach = _root.Q<Button>("segment-approach");
            _segmentOrbit = _root.Q<Button>("segment-orbit");
            _segmentMine = _root.Q<Button>("segment-mine");
            _segmentKeepAtRange = _root.Q<Button>("segment-keep-at-range");
            _segmentDock = _root.Q<Button>("segment-dock");
            _segmentLockTarget = _root.Q<Button>("segment-lock-target");

            // Query distance submenu elements
            _distanceSubmenu = _root.Q<VisualElement>("distance-submenu");
            _distanceSlider = _root.Q<SliderInt>("distance-slider");
            _distanceTitle = _root.Q<Label>("distance-title");
            _confirmButton = _root.Q<Button>("confirm-button");

            // Query preset buttons
            _preset25 = _root.Q<Button>("preset-25");
            _preset50 = _root.Q<Button>("preset-50");
            _preset100 = _root.Q<Button>("preset-100");
            _preset250 = _root.Q<Button>("preset-250");
            _preset500 = _root.Q<Button>("preset-500");

            // Bind segment click callbacks
            _segmentApproach?.RegisterCallback<ClickEvent>(_ => OnSegmentClicked(ActionApproach));
            _segmentOrbit?.RegisterCallback<ClickEvent>(_ => OnSegmentClicked(ActionOrbit));
            _segmentMine?.RegisterCallback<ClickEvent>(_ => OnSegmentClicked(ActionMine));
            _segmentKeepAtRange?.RegisterCallback<ClickEvent>(_ => OnSegmentClicked(ActionKeepAtRange));
            _segmentDock?.RegisterCallback<ClickEvent>(_ => OnDockClicked());
            _segmentLockTarget?.RegisterCallback<ClickEvent>(_ => OnLockTargetClicked());

            // Bind preset button callbacks
            _preset25?.RegisterCallback<ClickEvent>(_ => OnPresetClicked(25));
            _preset50?.RegisterCallback<ClickEvent>(_ => OnPresetClicked(50));
            _preset100?.RegisterCallback<ClickEvent>(_ => OnPresetClicked(100));
            _preset250?.RegisterCallback<ClickEvent>(_ => OnPresetClicked(250));
            _preset500?.RegisterCallback<ClickEvent>(_ => OnPresetClicked(500));

            // Bind confirm button
            _confirmButton?.RegisterCallback<ClickEvent>(_ => OnConfirm());

            // Ensure menu is hidden on start
            _root.style.display = DisplayStyle.None;
        }

        private void OnDisable()
        {
            // Cancel async EventBus subscriptions
            _eventCts?.Cancel();
            _eventCts?.Dispose();
            _eventCts = null;

            // Unregister callbacks to avoid leaks
            _segmentApproach?.UnregisterCallback<ClickEvent>(_ => OnSegmentClicked(ActionApproach));
            _segmentOrbit?.UnregisterCallback<ClickEvent>(_ => OnSegmentClicked(ActionOrbit));
            _segmentMine?.UnregisterCallback<ClickEvent>(_ => OnSegmentClicked(ActionMine));
            _segmentKeepAtRange?.UnregisterCallback<ClickEvent>(_ => OnSegmentClicked(ActionKeepAtRange));

            _preset25?.UnregisterCallback<ClickEvent>(_ => OnPresetClicked(25));
            _preset50?.UnregisterCallback<ClickEvent>(_ => OnPresetClicked(50));
            _preset100?.UnregisterCallback<ClickEvent>(_ => OnPresetClicked(100));
            _preset250?.UnregisterCallback<ClickEvent>(_ => OnPresetClicked(250));
            _preset500?.UnregisterCallback<ClickEvent>(_ => OnPresetClicked(500));

            _confirmButton?.UnregisterCallback<ClickEvent>(_ => OnConfirm());
        }

        private void Update()
        {
            if (!_isOpen) return;

            // Escape key closes the menu
            if (Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame)
            {
                Close();
            }
        }

        /// <summary>
        /// Opens the radial menu at the current mouse position.
        /// Called externally (e.g., by InputBridge on right-click).
        /// </summary>
        public void Open()
        {
            if (_root == null) return;

            // Suppress radial menu while docked — station services menu handles undock
            bool isDocked = _stateStore?.Current.Loop.Docking.IsDocked ?? false;
            if (isDocked) return;

            // Position menu at mouse cursor, clamped to screen bounds
            var mouse = Mouse.current;
            if (mouse == null) return;
            var mousePos = mouse.position.ReadValue();
            // Convert screen coords to UI Toolkit coords (Y is flipped)
            float uiX = mousePos.x - 150f; // Center the 300px menu
            float uiY = Screen.height - mousePos.y - 150f;

            // Clamp so the 300×300 radial + 530px-wide distance submenu stay on screen
            const float menuSize = 300f;
            const float totalWidth = 530f; // radial (300) + submenu offset (310) + submenu width (220)
            uiX = Mathf.Clamp(uiX, 0f, Mathf.Max(0f, Screen.width - totalWidth));
            uiY = Mathf.Clamp(uiY, 0f, Mathf.Max(0f, Screen.height - menuSize));

            _root.style.left = uiX;
            _root.style.top = uiY;
            _root.style.display = DisplayStyle.Flex;

            // Reset state
            _selectedAction = -1;
            HideDistanceSubmenu();
            ClearSegmentSelection();

            // Context-sensitive segment visibility
            // Lock Target is visible for all target types (FR-034)
            SetSegmentVisible(_segmentLockTarget, true);

            if (_currentTargetType == TargetType.Station)
            {
                // Station undocked: Approach, KeepAtRange, Orbit, Lock, Dock
                SetSegmentVisible(_segmentApproach, true);
                SetSegmentVisible(_segmentOrbit, true);
                SetSegmentVisible(_segmentMine, false);
                SetSegmentVisible(_segmentKeepAtRange, true);
                SetSegmentVisible(_segmentDock, true);
            }
            else
            {
                // Asteroid or default: Approach, Orbit, Mine, KeepAtRange, Lock
                SetSegmentVisible(_segmentApproach, true);
                SetSegmentVisible(_segmentOrbit, true);
                SetSegmentVisible(_segmentMine, true);
                SetSegmentVisible(_segmentKeepAtRange, true);
                SetSegmentVisible(_segmentDock, false);
            }

            _isOpen = true;
            _inputBridge?.SetRadialMenuOpen(true);
        }

        /// <summary>
        /// Closes the radial menu and resets state.
        /// </summary>
        public void Close()
        {
            if (_root == null) return;

            _root.style.display = DisplayStyle.None;
            _selectedAction = -1;
            _isOpen = false;
            _inputBridge?.SetRadialMenuOpen(false);
            HideDistanceSubmenu();
            ClearSegmentSelection();
        }

        /// <summary>
        /// Whether the radial menu is currently open.
        /// </summary>
        public bool IsOpen => _isOpen;

        private void OnSegmentClicked(int action)
        {
            _selectedAction = action;
            ClearSegmentSelection();

            // Mine action is immediate — no distance needed
            if (action == ActionMine)
            {
                _inputBridge?.StartMiningFromRadial();
                Close();
                return;
            }

            // For Approach, Orbit, KeepAtRange — show distance submenu
            HighlightSegment(action);
            ShowDistanceSubmenu(action);
        }

        private void OnPresetClicked(int distance)
        {
            if (_distanceSlider != null)
            {
                _distanceSlider.value = distance;
            }
        }

        private void OnConfirm()
        {
            if (_selectedAction < 0 || _distanceSlider == null) return;

            float distance = _distanceSlider.value;

            // Persist the chosen distance as the new default for this action
            switch (_selectedAction)
            {
                case ActionApproach:
                    _approachDistance = distance;
                    break;
                case ActionOrbit:
                    _orbitDistance = distance;
                    break;
                case ActionKeepAtRange:
                    _keepAtRangeDistance = distance;
                    break;
            }

            // Set radial action for ECS autopilot + align point for target
            _inputBridge?.SetRadialChoice(_selectedAction, distance);
            _inputBridge?.ApproachSelectedTarget(distance);
            Close();
        }

        private void ShowDistanceSubmenu(int action)
        {
            if (_distanceSubmenu == null) return;

            // Set slider to the persisted default for this action
            int defaultDistance = action switch
            {
                ActionApproach => (int)_approachDistance,
                ActionOrbit => (int)_orbitDistance,
                ActionKeepAtRange => (int)_keepAtRangeDistance,
                _ => 50
            };

            if (_distanceSlider != null)
            {
                _distanceSlider.value = defaultDistance;
            }

            // Update title to reflect action
            if (_distanceTitle != null)
            {
                _distanceTitle.text = action switch
                {
                    ActionApproach => "Approach Distance",
                    ActionOrbit => "Orbit Distance",
                    ActionKeepAtRange => "Keep at Range",
                    _ => "Set Distance"
                };
            }

            _distanceSubmenu.style.display = DisplayStyle.Flex;
        }

        private void HideDistanceSubmenu()
        {
            if (_distanceSubmenu != null)
            {
                _distanceSubmenu.style.display = DisplayStyle.None;
            }
        }

        private void HighlightSegment(int action)
        {
            var button = GetSegmentButton(action);
            button?.AddToClassList("radial-segment--selected");
        }

        private void OnDockClicked()
        {
            if (_inputBridge == null || _stateStore == null) return;

            var port = _inputBridge.GetSelectedDockingPort();
            if (port == null) return;

            // Immediate action — no distance submenu
            _inputBridge.SetRadialChoice(ActionDock, 0f);

            // Dispatch BeginDockingAction via state store
            _stateStore.Dispatch(new BeginDockingAction(
                port.StationId,
                port.WorldPortPosition,
                port.WorldPortRotation
            ));

            // Initiate docking at ECS level
            _inputBridge.InitiateDocking(port);

            // Publish docking started event
            _eventBus?.Publish(new DockingStartedEvent(port.StationId));

            Close();
        }

        private void OnLockTargetClicked()
        {
            if (_targetingController == null)
            {
                Debug.LogWarning("[RadialMenuController] TargetingController not found, Lock Target disabled");
                return;
            }
            _targetingController.AttemptLockOnSelected();
            Close();
        }

        private static void SetSegmentVisible(Button segment, bool visible)
        {
            if (segment == null) return;
            segment.style.display = visible ? DisplayStyle.Flex : DisplayStyle.None;
        }

        private void ClearSegmentSelection()
        {
            _segmentApproach?.RemoveFromClassList("radial-segment--selected");
            _segmentOrbit?.RemoveFromClassList("radial-segment--selected");
            _segmentMine?.RemoveFromClassList("radial-segment--selected");
            _segmentKeepAtRange?.RemoveFromClassList("radial-segment--selected");
            _segmentDock?.RemoveFromClassList("radial-segment--selected");
            _segmentLockTarget?.RemoveFromClassList("radial-segment--selected");
        }

        private Button GetSegmentButton(int action)
        {
            return action switch
            {
                ActionApproach => _segmentApproach,
                ActionOrbit => _segmentOrbit,
                ActionMine => _segmentMine,
                ActionKeepAtRange => _segmentKeepAtRange,
                ActionDock => _segmentDock,
                ActionLockTarget => _segmentLockTarget,
                _ => null
            };
        }
    }
}
