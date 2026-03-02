using System.Threading;
using UnityEngine;
using UnityEngine.UIElements;
using Unity.Mathematics;
using Cysharp.Threading.Tasks;
using VContainer;
using VoidHarvest.Core.EventBus;
using VoidHarvest.Core.EventBus.Events;
using VoidHarvest.Core.State;
using VoidHarvest.Features.Mining.Data;
using VoidHarvest.Features.Mining.Systems;

namespace VoidHarvest.Features.HUD.Views
{
    /// <summary>
    /// Reads StateStore and updates HUD UI Toolkit elements.
    /// Subscribes to state changes for resource counts, velocity, hull integrity, mining info.
    /// Progress bar shows depletion %, color transitions ore-to-red, pulses in sync with vein glow,
    /// flashes white on threshold crossings.
    /// View layer only — no game state stored here.
    /// See MVP-09: HUD updates within 1 frame.
    /// </summary>
    public sealed class HUDView : MonoBehaviour
    {
        [SerializeField] private UIDocument uiDocument;

        private IStateStore _stateStore;
        private IEventBus _eventBus;
        private DepletionVFXConfig _depletionConfig;
        private CancellationTokenSource _eventCts;
        private int _lastVersion = -1;

        // UI elements
        private Label _velocityLabel;
        private VisualElement _hotbarPanel;
        private VisualElement _shipInfoPanel;
        private VisualElement _hullBarFill;
        private VisualElement _resourceList;
        private VisualElement _miningPanel;
        private Label _miningOreLabel;
        private Label _miningYieldLabel;

        // Progress bar elements
        private VisualElement _progressBar;
        private VisualElement _progressFill;
        private VisualElement _progressFlash;

        // Flash state
        private float _flashTimer;
        private const float FlashDuration = 0.3f;

        /// <summary>
        /// DI injection point for the state store. See MVP-09: HUD updates within 1 frame.
        /// </summary>
        [Inject]
        public void Construct(IStateStore stateStore, IEventBus eventBus, DepletionVFXConfig depletionConfig)
        {
            _stateStore = stateStore;
            _eventBus = eventBus;
            _depletionConfig = depletionConfig;
        }

        private void OnEnable()
        {
            if (uiDocument == null) return;
            var root = uiDocument.rootVisualElement;

            _hotbarPanel = root.Q<VisualElement>("hotbar-panel");
            _shipInfoPanel = root.Q<VisualElement>("ship-info-panel");
            _velocityLabel = root.Q<Label>("velocity-label");
            _hullBarFill = root.Q<VisualElement>("hull-bar-fill");
            _resourceList = root.Q<VisualElement>("resource-list");
            _miningPanel = root.Q<VisualElement>("mining-info-panel");
            _miningOreLabel = root.Q<Label>("mining-ore-label");
            _miningYieldLabel = root.Q<Label>("mining-yield-label");
            _progressBar = root.Q<VisualElement>("mining-progress-bar");
            _progressFill = root.Q<VisualElement>("mining-progress-fill");
            _progressFlash = root.Q<VisualElement>("mining-progress-flash");
        }

        private void Start()
        {
            if (_eventBus != null)
            {
                _eventCts = new CancellationTokenSource();
                SubscribeToThresholdEvents(_eventCts.Token).Forget();
                ListenForDockingCompleted(_eventCts.Token).Forget();
                ListenForUndockingStarted(_eventCts.Token).Forget();
            }
        }

        private void OnDestroy()
        {
            _eventCts?.Cancel();
            _eventCts?.Dispose();
        }

        private async UniTaskVoid SubscribeToThresholdEvents(CancellationToken ct)
        {
            await foreach (var evt in _eventBus.Subscribe<ThresholdCrossedEvent>().WithCancellation(ct))
            {
                _flashTimer = FlashDuration;
            }
        }

        private async UniTaskVoid ListenForDockingCompleted(CancellationToken ct)
        {
            await foreach (var evt in _eventBus.Subscribe<DockingCompletedEvent>().WithCancellation(ct))
            {
                SetDockedVisibility(false);
            }
        }

        private async UniTaskVoid ListenForUndockingStarted(CancellationToken ct)
        {
            await foreach (var evt in _eventBus.Subscribe<UndockingStartedEvent>().WithCancellation(ct))
            {
                SetDockedVisibility(true);
            }
        }

        private void SetDockedVisibility(bool visible)
        {
            var display = visible ? DisplayStyle.Flex : DisplayStyle.None;
            if (_hotbarPanel != null) _hotbarPanel.style.display = display;
            if (_shipInfoPanel != null) _shipInfoPanel.style.display = display;
        }

        private void LateUpdate()
        {
            if (_stateStore == null) return;

            // Update flash timer (runs every frame regardless of version)
            if (_flashTimer > 0f)
            {
                _flashTimer -= Time.deltaTime;
                if (_progressFlash != null)
                {
                    float flashAlpha = Mathf.Clamp01(_flashTimer / FlashDuration);
                    _progressFlash.style.backgroundColor = new Color(1f, 1f, 1f, flashAlpha);
                }
            }

            if (_stateStore.Version == _lastVersion) return;
            _lastVersion = _stateStore.Version;

            var gameState = _stateStore.Current;

            // Velocity
            if (_velocityLabel != null)
            {
                float speed = math.length(gameState.ActiveShipPhysics.Velocity);
                _velocityLabel.text = $"VEL: {speed:F0} m/s";
            }

            // Hull integrity
            if (_hullBarFill != null)
            {
                float hull = gameState.ActiveShipPhysics.HullIntegrity;
                _hullBarFill.style.width = new StyleLength(new Length(hull * 100f, LengthUnit.Percent));

                // Color: green > yellow > red
                Color hullColor;
                if (hull > 0.5f) hullColor = Color.Lerp(Color.yellow, Color.green, (hull - 0.5f) * 2f);
                else hullColor = Color.Lerp(Color.red, Color.yellow, hull * 2f);
                _hullBarFill.style.backgroundColor = hullColor;
            }

            // Resource list
            if (_resourceList != null)
            {
                _resourceList.Clear();
                foreach (var kvp in gameState.Loop.Inventory.Stacks)
                {
                    var label = new Label($"{OreDefinitionRegistry.GetDisplayName(kvp.Key)}: {kvp.Value.Quantity}");
                    label.AddToClassList("hud-label");
                    _resourceList.Add(label);
                }
            }

            // Mining info + progress bar
            if (_miningPanel != null)
            {
                var mining = gameState.Loop.Mining;
                if (mining.TargetAsteroidId.HasValue)
                {
                    _miningPanel.style.display = DisplayStyle.Flex;
                    if (_miningOreLabel != null)
                        _miningOreLabel.text = $"Mining: {OreDisplayNames.Get(mining.ActiveOreId.GetValueOrDefault("Unknown"))}";
                    if (_miningYieldLabel != null)
                        _miningYieldLabel.text = $"Yield: {mining.YieldAccumulator:F1} units";

                    UpdateProgressBar(mining.DepletionFraction);
                }
                else
                {
                    _miningPanel.style.display = DisplayStyle.None;
                }
            }
        }

        private void UpdateProgressBar(float depletion)
        {
            if (_progressFill == null) return;

            // Fill width from depletion fraction
            float percent = depletion * 100f;
            _progressFill.style.width = new StyleLength(new Length(percent, LengthUnit.Percent));

            // Color lerp: ore color (green-ish) to red-orange via formula
            var oreColor = new Color(0.3f, 0.8f, 0.2f, 1f);
            var fillColor = MiningVFXFormulas.CalculateDepletionColor(oreColor, depletion);

            // Pulse opacity synced with vein glow
            float pulseSpeed = _depletionConfig != null ? _depletionConfig.VeinGlowPulseSpeed : 1.5f;
            float pulseAmplitude = _depletionConfig != null ? _depletionConfig.VeinGlowPulseAmplitude : 0.25f;
            float pulseAlpha = MiningVFXFormulas.ApplyPulseModulation(1f, pulseSpeed, pulseAmplitude, Time.time);
            fillColor.a = Mathf.Clamp01(pulseAlpha);

            _progressFill.style.backgroundColor = fillColor;
        }
    }
}
