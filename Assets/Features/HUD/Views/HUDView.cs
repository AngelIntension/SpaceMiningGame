using UnityEngine;
using UnityEngine.UIElements;
using Unity.Mathematics;
using VContainer;
using VoidHarvest.Core.State;

namespace VoidHarvest.Features.HUD.Views
{
    /// <summary>
    /// Reads StateStore and updates HUD UI Toolkit elements.
    /// Subscribes to state changes for resource counts, velocity, hull integrity, mining info.
    /// View layer only — no game state stored here.
    /// See MVP-09: HUD updates within 1 frame.
    /// </summary>
    public sealed class HUDView : MonoBehaviour
    {
        [SerializeField] private UIDocument uiDocument;

        private IStateStore _stateStore;
        private int _lastVersion = -1;

        // UI elements
        private Label _velocityLabel;
        private VisualElement _hullBarFill;
        private VisualElement _resourceList;
        private VisualElement _miningPanel;
        private Label _miningOreLabel;
        private Label _miningYieldLabel;

        /// <summary>
        /// DI injection point for the state store. See MVP-09: HUD updates within 1 frame.
        /// </summary>
        [Inject]
        public void Construct(IStateStore stateStore)
        {
            _stateStore = stateStore;
        }

        private void OnEnable()
        {
            if (uiDocument == null) return;
            var root = uiDocument.rootVisualElement;

            _velocityLabel = root.Q<Label>("velocity-label");
            _hullBarFill = root.Q<VisualElement>("hull-bar-fill");
            _resourceList = root.Q<VisualElement>("resource-list");
            _miningPanel = root.Q<VisualElement>("mining-info-panel");
            _miningOreLabel = root.Q<Label>("mining-ore-label");
            _miningYieldLabel = root.Q<Label>("mining-yield-label");
        }

        private void LateUpdate()
        {
            if (_stateStore == null) return;
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
                    var label = new Label($"{kvp.Key}: {kvp.Value.Quantity}");
                    label.AddToClassList("hud-label");
                    _resourceList.Add(label);
                }
            }

            // Mining info
            if (_miningPanel != null)
            {
                var mining = gameState.Loop.Mining;
                if (mining.TargetAsteroidId.HasValue)
                {
                    _miningPanel.style.display = DisplayStyle.Flex;
                    if (_miningOreLabel != null)
                        _miningOreLabel.text = $"Mining: {mining.ActiveOreId.GetValueOrDefault("Unknown")}";
                    if (_miningYieldLabel != null)
                        _miningYieldLabel.text = $"Yield: {mining.YieldAccumulator:F1} units";
                }
                else
                {
                    _miningPanel.style.display = DisplayStyle.None;
                }
            }
        }
    }
}
