using UnityEngine;
using UnityEngine.UIElements;
using VoidHarvest.Core.EventBus;
using VoidHarvest.Core.State;
using VoidHarvest.Features.Targeting.Data;
using VoidHarvest.Features.Targeting.Systems;
using Background = UnityEngine.UIElements.Background;

namespace VoidHarvest.Features.Targeting.Views
{
    /// <summary>
    /// Manages a single target card VisualElement. Displays name, type, range,
    /// a dismiss button, and a viewport placeholder (solid dark rectangle for now).
    /// See Spec 007: In-Flight Targeting (FR-027, FR-023, FR-023a, FR-031).
    /// </summary>
    public sealed class TargetCardView
    {
        private readonly IStateStore _stateStore;
        private readonly IEventBus _eventBus;
        private readonly VisualElement _root;
        private readonly VisualElement _viewport;
        private readonly Label _nameLabel;
        private readonly Label _rangeLabel;
        private readonly int _targetId;
        private readonly TargetLockData _lockData;
        private readonly TargetPreviewManager _previewManager;
        private RenderTexture _previewRT;

        public VisualElement Root => _root;
        public int TargetId => _targetId;

        public TargetCardView(TargetLockData lockData, IStateStore stateStore,
            IEventBus eventBus, TargetPreviewManager previewManager)
        {
            _lockData = lockData;
            _targetId = lockData.TargetId;
            _stateStore = stateStore;
            _eventBus = eventBus;
            _previewManager = previewManager;

            _root = new VisualElement();
            _root.AddToClassList("target-card");

            // Viewport — live RenderTexture if preview manager available, else solid placeholder
            _viewport = new VisualElement();
            _viewport.AddToClassList("target-card-viewport");

            if (_previewManager != null)
            {
                _previewRT = _previewManager.GetOrCreatePreview(_targetId, lockData.TargetType);
                if (_previewRT != null)
                    _viewport.style.backgroundImage = new StyleBackground(Background.FromRenderTexture(_previewRT));
            }

            _root.Add(_viewport);

            // Dismiss button
            var dismiss = new Button { text = "X" };
            dismiss.AddToClassList("target-card-dismiss");
            dismiss.clicked += OnDismiss;
            _root.Add(dismiss);

            // Info section
            var info = new VisualElement();
            info.AddToClassList("target-card-info");

            _nameLabel = new Label(lockData.DisplayName);
            _nameLabel.AddToClassList("target-card-name");
            info.Add(_nameLabel);

            _rangeLabel = new Label("");
            _rangeLabel.AddToClassList("target-card-range");
            info.Add(_rangeLabel);

            _root.Add(info);

            // Card body click → select this locked target (FR-023a)
            _root.RegisterCallback<ClickEvent>(OnCardClicked);
        }

        public void UpdateRange(Vector3 targetPos, Vector3 shipPos)
        {
            float range = Vector3.Distance(shipPos, targetPos);
            _rangeLabel.text = TargetingMath.FormatRange(range);

            // Reassign RT as background each frame to force UI Toolkit to re-read content
            if (_previewRT != null)
            {
                _viewport.style.backgroundImage = new StyleBackground(
                    Background.FromRenderTexture(_previewRT));
                _viewport.MarkDirtyRepaint();
            }
        }

        private void OnDismiss()
        {
            _stateStore?.Dispatch(new UnlockTargetAction(_targetId));
            var evt = new TargetUnlockedEvent(_targetId);
            _eventBus?.Publish(in evt);
        }

        private void OnCardClicked(ClickEvent evt)
        {
            // Only if click wasn't on the dismiss button
            if (evt.target is Button) return;

            _stateStore?.Dispatch(new SelectTargetAction(
                _lockData.TargetId, _lockData.TargetType,
                _lockData.DisplayName, _lockData.TypeLabel));
        }

        public void Dispose()
        {
            _root.UnregisterCallback<ClickEvent>(OnCardClicked);
            _previewManager?.ReleasePreview(_targetId);
        }
    }
}
