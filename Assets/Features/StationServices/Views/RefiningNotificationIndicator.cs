using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.UIElements;
using VContainer;
using VoidHarvest.Core.EventBus;
using VoidHarvest.Features.StationServices.Data;

namespace VoidHarvest.Features.StationServices.Views
{
    /// <summary>
    /// HUD indicator for pending completed refining jobs.
    /// Visible when count > 0, hidden when all collected.
    /// Subscribes to RefiningJobCompletedEvent and CollectRefiningJobAction events.
    /// See Spec 006 US6: Refining Job Notifications, FR-044.
    /// </summary>
    public sealed class RefiningNotificationIndicator : MonoBehaviour
    {
        [SerializeField] private UIDocument hudDocument;

        private IEventBus _eventBus;
        private CancellationTokenSource _cts;
        private RefiningNotificationTracker _tracker;
        private Label _badge;

        [Inject]
        public void Construct(IEventBus eventBus)
        {
            _eventBus = eventBus;
        }

        private void Start()
        {
            _tracker = new RefiningNotificationTracker();

            if (hudDocument != null)
            {
                _badge = hudDocument.rootVisualElement?.Q<Label>("refining-badge");
            }

            if (_eventBus != null)
            {
                _cts = new CancellationTokenSource();
                ListenForCompleted(_cts.Token).Forget();
                ListenForCollected(_cts.Token).Forget();
            }

            UpdateBadge();
        }

        private void OnDestroy()
        {
            _cts?.Cancel();
            _cts?.Dispose();
        }

        private async UniTaskVoid ListenForCompleted(CancellationToken ct)
        {
            await foreach (var evt in _eventBus.Subscribe<RefiningJobCompletedEvent>().WithCancellation(ct))
            {
                _tracker.OnJobCompleted(evt.StationId, evt.JobId);
                UpdateBadge();
            }
        }

        private async UniTaskVoid ListenForCollected(CancellationToken ct)
        {
            await foreach (var evt in _eventBus.Subscribe<RefiningJobCollectedEvent>().WithCancellation(ct))
            {
                _tracker.OnJobCollected(evt.StationId, evt.JobId);
                UpdateBadge();
            }
        }

        private void UpdateBadge()
        {
            if (_badge == null) return;

            if (_tracker.HasPending)
            {
                _badge.text = $"{_tracker.PendingCount} refining job{(_tracker.PendingCount > 1 ? "s" : "")} complete";
                _badge.style.display = DisplayStyle.Flex;
            }
            else
            {
                _badge.style.display = DisplayStyle.None;
            }
        }
    }
}
