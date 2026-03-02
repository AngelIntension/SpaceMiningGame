using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.UIElements;
using VContainer;
using VoidHarvest.Core.EventBus;
using VoidHarvest.Core.State;

namespace VoidHarvest.Features.StationServices.Views
{
    /// <summary>
    /// Persistent credit balance indicator in the station services menu header.
    /// See Spec 006 FR-050, FR-051.
    /// </summary>
    public sealed class CreditBalanceIndicator : MonoBehaviour
    {
        private IStateStore _stateStore;
        private IEventBus _eventBus;
        private Label _creditLabel;
        private CancellationTokenSource _stateCts;

        [Inject]
        public void Construct(IStateStore stateStore, IEventBus eventBus)
        {
            _stateStore = stateStore;
            _eventBus = eventBus;
        }

        public void Initialize(Label creditLabel)
        {
            _creditLabel = creditLabel;

            _stateCts?.Cancel();
            _stateCts = new CancellationTokenSource();
            ListenForStateChanges(_stateCts.Token).Forget();

            UpdateDisplay();
        }

        public void Cleanup()
        {
            _stateCts?.Cancel();
            _stateCts?.Dispose();
            _stateCts = null;
        }

        private async UniTaskVoid ListenForStateChanges(CancellationToken ct)
        {
            await foreach (var _ in _stateStore.OnStateChanged.WithCancellation(ct))
            {
                UpdateDisplay();
            }
        }

        private void UpdateDisplay()
        {
            if (_stateStore == null || _creditLabel == null) return;
            int credits = _stateStore.Current.Loop.StationServices.Credits;
            _creditLabel.text = $"Credits: {credits}";
        }
    }
}
