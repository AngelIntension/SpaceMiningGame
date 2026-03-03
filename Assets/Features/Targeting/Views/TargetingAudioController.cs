using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;
using VContainer;
using VoidHarvest.Core.EventBus;
using VoidHarvest.Core.State;
using VoidHarvest.Features.Targeting.Data;

namespace VoidHarvest.Features.Targeting.Views
{
    /// <summary>
    /// Plays audio feedback for targeting events: lock acquiring, confirmed,
    /// failed, slots full, and target lost.
    /// See Spec 007: In-Flight Targeting (FR-011, FR-012, FR-014).
    /// </summary>
    public sealed class TargetingAudioController : MonoBehaviour
    {
        private IEventBus _eventBus;
        private IStateStore _stateStore;
        private TargetingAudioConfig _config;
        private AudioSource _audioSource;
        private CancellationTokenSource _eventCts;
        private bool _wasAcquiring;

        [Inject]
        public void Construct(IEventBus eventBus, IStateStore stateStore, TargetingAudioConfig config)
        {
            _eventBus = eventBus;
            _stateStore = stateStore;
            _config = config;
        }

        private void Start()
        {
            _audioSource = GetComponent<AudioSource>();
            if (_audioSource == null)
                _audioSource = gameObject.AddComponent<AudioSource>();
            _audioSource.playOnAwake = false;
            _audioSource.spatialBlend = 0f;
        }

        private void OnEnable()
        {
            if (_eventBus != null && _config != null)
            {
                _eventCts = new CancellationTokenSource();
                ListenForLocked(_eventCts.Token).Forget();
                ListenForFailed(_eventCts.Token).Forget();
                ListenForSlotsFull(_eventCts.Token).Forget();
                ListenForTargetLost(_eventCts.Token).Forget();
            }
        }

        private void OnDisable()
        {
            _eventCts?.Cancel();
            _eventCts?.Dispose();
            _eventCts = null;
        }

        private void LateUpdate()
        {
            if (_stateStore == null || _config == null || _audioSource == null) return;

            var acquisition = _stateStore.Current.Loop.Targeting.LockAcquisition;
            bool isAcquiring = acquisition.IsActive;

            if (isAcquiring && !_wasAcquiring)
            {
                if (_config.LockAcquiringClip != null)
                {
                    _audioSource.clip = _config.LockAcquiringClip;
                    _audioSource.loop = true;
                    _audioSource.Play();
                }
            }
            else if (!isAcquiring && _wasAcquiring)
            {
                if (_audioSource.isPlaying && _audioSource.clip == _config.LockAcquiringClip)
                    _audioSource.Stop();
            }

            // Pitch shift during acquisition
            if (isAcquiring && _config.LockAcquiringClip != null)
            {
                _audioSource.pitch = 0.8f + acquisition.Progress * 0.4f;
            }

            _wasAcquiring = isAcquiring;
        }

        private async UniTaskVoid ListenForLocked(CancellationToken ct)
        {
            await foreach (var _ in _eventBus.Subscribe<TargetLockedEvent>().WithCancellation(ct))
            {
                PlayOneShot(_config.LockConfirmedClip);
            }
        }

        private async UniTaskVoid ListenForFailed(CancellationToken ct)
        {
            await foreach (var _ in _eventBus.Subscribe<LockFailedEvent>().WithCancellation(ct))
            {
                PlayOneShot(_config.LockFailedClip);
            }
        }

        private async UniTaskVoid ListenForSlotsFull(CancellationToken ct)
        {
            await foreach (var _ in _eventBus.Subscribe<LockSlotsFullEvent>().WithCancellation(ct))
            {
                PlayOneShot(_config.LockSlotsFullClip);
            }
        }

        private async UniTaskVoid ListenForTargetLost(CancellationToken ct)
        {
            await foreach (var _ in _eventBus.Subscribe<TargetLostEvent>().WithCancellation(ct))
            {
                PlayOneShot(_config.TargetLostClip);
            }
        }

        private void PlayOneShot(AudioClip clip)
        {
            if (clip != null && _audioSource != null)
                _audioSource.PlayOneShot(clip);
        }
    }
}
