using UnityEngine;
using UnityEngine.UIElements;
using Cysharp.Threading.Tasks;
using VContainer;
using VoidHarvest.Core.EventBus;
using VoidHarvest.Core.EventBus.Events;

namespace VoidHarvest.Features.HUD.Views
{
    /// <summary>
    /// Displays mining-related warnings (Cargo Full, Out of Range, Asteroid Depleted).
    /// Subscribes to MiningStoppedEvent via EventBus; auto-dismisses after 3 seconds.
    /// View layer only — no game state stored here.
    /// See MVP-05: Mining beam and yield.
    /// </summary>
    public sealed class HUDWarningView : MonoBehaviour
    {
        [SerializeField] private UIDocument uiDocument;

        private IEventBus _eventBus;
        private Label _warningLabel;
        private VisualElement _warningPanel;

        /// <summary>
        /// DI injection point for the event bus. See MVP-05: Mining beam and yield.
        /// </summary>
        [Inject]
        public void Construct(IEventBus eventBus)
        {
            _eventBus = eventBus;
        }

        private void OnEnable()
        {
            if (uiDocument != null)
            {
                var root = uiDocument.rootVisualElement;
                _warningPanel = root.Q<VisualElement>("warning-panel");
                _warningLabel = root.Q<Label>("warning-text");

                if (_warningPanel != null)
                    _warningPanel.style.display = DisplayStyle.None;
            }
        }

        private void Start()
        {
            if (_eventBus != null)
                ListenForMiningStoppedAsync().Forget();
        }

        private async UniTaskVoid ListenForMiningStoppedAsync()
        {
            var token = this.GetCancellationTokenOnDestroy();

            await foreach (var evt in _eventBus.Subscribe<MiningStoppedEvent>().WithCancellation(token))
            {
                string message = evt.Reason switch
                {
                    StopReason.CargoFull => "CARGO FULL",
                    StopReason.OutOfRange => "OUT OF RANGE",
                    StopReason.AsteroidDepleted => "ASTEROID DEPLETED",
                    StopReason.PlayerStopped => null, // No warning for manual stop
                    _ => null
                };

                if (message != null)
                    ShowWarning(message).Forget();
            }
        }

        private async UniTaskVoid ShowWarning(string message)
        {
            if (_warningPanel == null || _warningLabel == null) return;

            _warningLabel.text = message;
            _warningPanel.style.display = DisplayStyle.Flex;

            await UniTask.Delay(3000, cancellationToken: this.GetCancellationTokenOnDestroy());

            _warningPanel.style.display = DisplayStyle.None;
        }
    }
}
