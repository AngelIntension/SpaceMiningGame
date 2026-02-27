using UnityEngine;
using UnityEngine.UIElements;
using VContainer;
using VoidHarvest.Core.State;

namespace VoidHarvest.Features.HUD.Views
{
    /// <summary>
    /// Displays target information panel when an asteroid is selected.
    /// View layer only — no game state stored here.
    /// See MVP-03: Target selection with info display.
    /// </summary>
    public sealed class TargetInfoPanel : MonoBehaviour
    {
        [SerializeField] private UIDocument uiDocument;

        private IStateStore _stateStore;
        private VisualElement _panelRoot;
        private Label _targetNameLabel;
        private Label _oreTypeLabel;
        private Label _distanceLabel;
        private Label _massLabel;
        private int _lastVersion = -1;

        /// <summary>
        /// DI injection point for the state store. See MVP-03: Target selection with info display.
        /// </summary>
        [Inject]
        public void Construct(IStateStore stateStore) { _stateStore = stateStore; }

        private void OnEnable()
        {
            if (uiDocument == null) return;
            var root = uiDocument.rootVisualElement;
            _panelRoot = root.Q<VisualElement>("target-info-panel");
            _targetNameLabel = root.Q<Label>("target-name");
            _oreTypeLabel = root.Q<Label>("target-ore-type");
            _distanceLabel = root.Q<Label>("target-distance");
            _massLabel = root.Q<Label>("target-mass");

            if (_panelRoot != null)
                _panelRoot.style.display = DisplayStyle.None;
        }

        private void LateUpdate()
        {
            if (_stateStore == null) return;
            if (_stateStore.Version == _lastVersion) return;
            _lastVersion = _stateStore.Version;

            // Check if there's a selected target
            // For now, show/hide based on whether mining is active or a target is selected
            // In MVP, target info will be populated when InputBridge sets a target
            // This is a placeholder that will be connected in T048

            // The panel reads from a future "SelectedTarget" field or event
            // For now, expose a public method to set target info
        }

        /// <summary>
        /// Called by InputBridge or another system when a target is selected.
        /// </summary>
        public void ShowTarget(string name, string oreType, float distance, float massPercent)
        {
            if (_panelRoot == null) return;
            _panelRoot.style.display = DisplayStyle.Flex;
            _targetNameLabel.text = name;
            _oreTypeLabel.text = oreType;
            _distanceLabel.text = $"{distance:F0}m";
            _massLabel.text = $"{massPercent:P0}";
        }

        /// <summary>
        /// Hide the target info panel. See MVP-03: Target selection with info display.
        /// </summary>
        public void HideTarget()
        {
            if (_panelRoot != null)
                _panelRoot.style.display = DisplayStyle.None;
        }
    }
}
