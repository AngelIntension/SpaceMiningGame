using UnityEngine;
using Cysharp.Threading.Tasks;
using VContainer;
using VoidHarvest.Core.EventBus;
using VoidHarvest.Core.EventBus.Events;
using VoidHarvest.Features.Docking.Data;

namespace VoidHarvest.Features.Docking.Views
{
    /// <summary>
    /// Manages VFX and audio feedback for docking sequences.
    /// Subscribes to docking events via EventBus; plays effects from injected configs.
    /// View layer only — no game state.
    /// See Spec 004 US5: Docking Audio & Visual Feedback.
    /// </summary>
    public sealed class DockingFeedbackView : MonoBehaviour
    {
        private DockingVFXConfig _vfxConfig;
        private DockingAudioConfig _audioConfig;
        private IEventBus _eventBus;

        private AudioSource _audioSource;
        private GameObject _activeApproachVFX;

        [Inject]
        public void Construct(DockingVFXConfig vfxConfig, DockingAudioConfig audioConfig, IEventBus eventBus)
        {
            _vfxConfig = vfxConfig;
            _audioConfig = audioConfig;
            _eventBus = eventBus;
        }

        private void Start()
        {
            _audioSource = GetComponent<AudioSource>();
            if (_audioSource == null)
            {
                _audioSource = gameObject.AddComponent<AudioSource>();
                _audioSource.playOnAwake = false;
                _audioSource.spatialBlend = 1f;
                if (_audioConfig != null)
                {
                    _audioSource.maxDistance = _audioConfig.MaxAudibleDistance;
                    _audioSource.rolloffMode = AudioRolloffMode.Linear;
                }
            }

            if (_eventBus != null)
            {
                SubscribeDockingStarted().Forget();
                SubscribeDockingCompleted().Forget();
                SubscribeDockingCancelled().Forget();
                SubscribeUndockingStarted().Forget();
                SubscribeUndockCompleted().Forget();
            }
        }

        private async UniTaskVoid SubscribeDockingStarted()
        {
            var ct = this.GetCancellationTokenOnDestroy();
            await foreach (var evt in _eventBus.Subscribe<DockingStartedEvent>().WithCancellation(ct))
            {
                OnDockingStarted();
            }
        }

        private async UniTaskVoid SubscribeDockingCompleted()
        {
            var ct = this.GetCancellationTokenOnDestroy();
            await foreach (var evt in _eventBus.Subscribe<DockingCompletedEvent>().WithCancellation(ct))
            {
                OnDockingCompleted();
            }
        }

        private async UniTaskVoid SubscribeDockingCancelled()
        {
            var ct = this.GetCancellationTokenOnDestroy();
            await foreach (var evt in _eventBus.Subscribe<DockingCancelledEvent>().WithCancellation(ct))
            {
                OnDockingCancelled();
            }
        }

        private async UniTaskVoid SubscribeUndockingStarted()
        {
            var ct = this.GetCancellationTokenOnDestroy();
            await foreach (var evt in _eventBus.Subscribe<UndockingStartedEvent>().WithCancellation(ct))
            {
                OnUndockingStarted();
            }
        }

        private async UniTaskVoid SubscribeUndockCompleted()
        {
            var ct = this.GetCancellationTokenOnDestroy();
            await foreach (var evt in _eventBus.Subscribe<UndockCompletedEvent>().WithCancellation(ct))
            {
                OnUndockCompleted();
            }
        }

        private void OnDockingStarted()
        {
            // Spawn alignment guide VFX
            if (_vfxConfig != null && _vfxConfig.AlignmentGuideEffect != null)
            {
                _activeApproachVFX = Instantiate(_vfxConfig.AlignmentGuideEffect, transform);
            }

            // Play approach hum audio
            if (_audioConfig != null && _audioConfig.ApproachHumClip != null && _audioSource != null)
            {
                _audioSource.clip = _audioConfig.ApproachHumClip;
                _audioSource.loop = true;
                _audioSource.Play();
            }
        }

        private void OnDockingCompleted()
        {
            // Clean up approach VFX
            CleanUpApproachVFX();

            // Play snap flash VFX
            if (_vfxConfig != null && _vfxConfig.SnapFlashEffect != null)
            {
                var flash = Instantiate(_vfxConfig.SnapFlashEffect, transform.position, transform.rotation);
                Destroy(flash, _vfxConfig.SnapFlashDuration);
            }

            // Play dock clamp audio
            if (_audioConfig != null && _audioConfig.DockClampClip != null && _audioSource != null)
            {
                _audioSource.loop = false;
                _audioSource.Stop();
                _audioSource.PlayOneShot(_audioConfig.DockClampClip, _audioConfig.DockClampVolume);
            }
        }

        private void OnDockingCancelled()
        {
            // Clean up approach VFX
            CleanUpApproachVFX();

            // Stop approach audio
            if (_audioSource != null)
            {
                _audioSource.loop = false;
                _audioSource.Stop();
            }
        }

        private void OnUndockingStarted()
        {
            // Play engine start audio
            if (_audioConfig != null && _audioConfig.EngineStartClip != null && _audioSource != null)
            {
                _audioSource.PlayOneShot(_audioConfig.EngineStartClip, _audioConfig.UndockReleaseVolume);
            }
        }

        private void OnUndockCompleted()
        {
            // Play undock release VFX
            if (_vfxConfig != null && _vfxConfig.UndockReleaseEffect != null)
            {
                var release = Instantiate(_vfxConfig.UndockReleaseEffect, transform.position, transform.rotation);
                Destroy(release, 2f);
            }

            // Play undock release audio
            if (_audioConfig != null && _audioConfig.UndockReleaseClip != null && _audioSource != null)
            {
                _audioSource.PlayOneShot(_audioConfig.UndockReleaseClip, _audioConfig.UndockReleaseVolume);
            }
        }

        private void CleanUpApproachVFX()
        {
            if (_activeApproachVFX != null)
            {
                Destroy(_activeApproachVFX);
                _activeApproachVFX = null;
            }
        }
    }
}
