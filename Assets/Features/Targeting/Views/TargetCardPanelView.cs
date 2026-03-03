using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;
using VoidHarvest.Core.EventBus;
using VoidHarvest.Core.State;

namespace VoidHarvest.Features.Targeting.Views
{
    /// <summary>
    /// Manages the horizontal panel of target cards. Creates/destroys card views
    /// to match LockedTargets count. Cards appear on the right, reflow on removal.
    /// See Spec 007: In-Flight Targeting (FR-026, FR-029, FR-030).
    /// </summary>
    public sealed class TargetCardPanelView
    {
        private readonly IStateStore _stateStore;
        private readonly IEventBus _eventBus;
        private readonly VisualElement _panel;
        private readonly TargetPreviewManager _previewManager;
        private readonly List<TargetCardView> _cards = new List<TargetCardView>();

        public TargetCardPanelView(VisualElement root, IStateStore stateStore,
            IEventBus eventBus, TargetPreviewManager previewManager)
        {
            _stateStore = stateStore;
            _eventBus = eventBus;
            _previewManager = previewManager;
            _panel = root.Q("target-cards-panel");
        }

        public void Update(
            System.Collections.Immutable.ImmutableArray<TargetLockData> lockedTargets,
            TargetingController controller, Vector3 shipPos)
        {
            if (_panel == null) return;

            // Remove cards for targets no longer locked
            for (int i = _cards.Count - 1; i >= 0; i--)
            {
                bool found = false;
                for (int j = 0; j < lockedTargets.Length; j++)
                {
                    if (lockedTargets[j].TargetId == _cards[i].TargetId)
                    {
                        found = true;
                        break;
                    }
                }

                if (!found)
                {
                    _cards[i].Dispose();
                    _panel.Remove(_cards[i].Root);
                    _cards.RemoveAt(i);
                }
            }

            // Add cards for newly locked targets
            for (int i = 0; i < lockedTargets.Length; i++)
            {
                bool exists = false;
                for (int j = 0; j < _cards.Count; j++)
                {
                    if (_cards[j].TargetId == lockedTargets[i].TargetId)
                    {
                        exists = true;
                        break;
                    }
                }

                if (!exists)
                {
                    var card = new TargetCardView(lockedTargets[i], _stateStore, _eventBus, _previewManager);
                    _cards.Add(card);
                    _panel.Add(card.Root);
                }
            }

            // Update range on all cards
            for (int i = 0; i < _cards.Count; i++)
            {
                if (controller != null && controller.GetTargetWorldPosition(
                    _cards[i].TargetId, out var targetPos, out _))
                {
                    _cards[i].UpdateRange(targetPos, shipPos);
                }
            }

            _panel.style.display = _cards.Count > 0 ? DisplayStyle.Flex : DisplayStyle.None;
        }

        public void ClearAll()
        {
            for (int i = _cards.Count - 1; i >= 0; i--)
            {
                _cards[i].Dispose();
                _panel?.Remove(_cards[i].Root);
            }
            _cards.Clear();

            if (_panel != null)
                _panel.style.display = DisplayStyle.None;
        }
    }
}
