using UnityEngine;
using Cysharp.Threading.Tasks;
using VContainer;
using VoidHarvest.Core.EventBus;
using VoidHarvest.Core.EventBus.Events;
using VoidHarvest.Core.State;
using VoidHarvest.Features.Mining.Data;
using VoidHarvest.Features.Mining.Systems;

namespace VoidHarvest.Features.Mining.Views
{
    /// <summary>
    /// Manages spatial audio for all mining audio cues.
    /// 3 AudioSources: HumSource (looping at beam midpoint), ImpactSource (one-shot at asteroid),
    /// EventSource (one-shot, repositioned per event).
    /// Subscribes to mining events and drives pitch/volume from state.
    /// </summary>
    public sealed class MiningAudioController : MonoBehaviour
    {
        private MiningAudioConfig _config;
        private IEventBus _eventBus;
        private IStateStore _stateStore;

        private AudioSource _humSource;
        private AudioSource _impactSource;
        private AudioSource _eventSource;

        private bool _isMining;
        private bool _isFadingOut;
        private float _fadeElapsed;

        [Inject]
        public void Construct(MiningAudioConfig config, IEventBus eventBus, IStateStore stateStore)
        {
            _config = config;
            _eventBus = eventBus;
            _stateStore = stateStore;
        }

        private void Start()
        {
            _humSource = CreateAudioSource("HumSource", true);
            _impactSource = CreateAudioSource("ImpactSource", false);
            _eventSource = CreateAudioSource("EventSource", false);

            if (_eventBus != null)
            {
                SubscribeStarted().Forget();
                SubscribeStopped().Forget();
                SubscribeThreshold().Forget();
                SubscribeCollection().Forget();
            }
        }

        private AudioSource CreateAudioSource(string name, bool loop)
        {
            var go = new GameObject(name);
            go.transform.SetParent(transform);
            var source = go.AddComponent<AudioSource>();
            source.loop = loop;
            source.playOnAwake = false;
            source.spatialBlend = 1f; // Full 3D
            if (_config != null)
            {
                source.maxDistance = _config.MaxAudibleDistance;
                source.rolloffMode = AudioRolloffMode.Linear;
            }
            return source;
        }

        private async UniTaskVoid SubscribeStarted()
        {
            var cts = this.GetCancellationTokenOnDestroy();
            await foreach (var evt in _eventBus.Subscribe<MiningStartedEvent>().WithCancellation(cts))
            {
                OnMiningStarted();
            }
        }

        private async UniTaskVoid SubscribeStopped()
        {
            var cts = this.GetCancellationTokenOnDestroy();
            await foreach (var evt in _eventBus.Subscribe<MiningStoppedEvent>().WithCancellation(cts))
            {
                OnMiningStopped();
            }
        }

        private async UniTaskVoid SubscribeThreshold()
        {
            var cts = this.GetCancellationTokenOnDestroy();
            await foreach (var evt in _eventBus.Subscribe<ThresholdCrossedEvent>().WithCancellation(cts))
            {
                OnThresholdCrossed(evt);
            }
        }

        private async UniTaskVoid SubscribeCollection()
        {
            var cts = this.GetCancellationTokenOnDestroy();
            await foreach (var evt in _eventBus.Subscribe<OreChunkCollectedEvent>().WithCancellation(cts))
            {
                OnChunkCollected(evt);
            }
        }

        private void OnMiningStarted()
        {
            _isMining = true;
            _isFadingOut = false;
            _fadeElapsed = 0f;

            var humClip = _config != null ? _config.LaserHumClip : null;
            if (humClip == null) humClip = ProceduralAudioGenerator.LaserHum;
            _humSource.clip = humClip;
            _humSource.volume = _config != null ? _config.LaserHumBaseVolume : 0.6f;
            _humSource.Play();

            var sparkClip = _config != null ? _config.SparkCrackleClip : null;
            if (sparkClip == null) sparkClip = ProceduralAudioGenerator.SparkCrackle;
            _impactSource.clip = sparkClip;
            _impactSource.volume = _config != null ? _config.SparkCrackleVolume : 0.4f;
            _impactSource.loop = true;
            _impactSource.Play();
        }

        private void OnMiningStopped()
        {
            _isMining = false;
            _isFadingOut = true;
            _fadeElapsed = 0f;

            _impactSource.Stop();
            _impactSource.loop = false;
        }

        private void OnThresholdCrossed(ThresholdCrossedEvent evt)
        {
            var pos = new Vector3(evt.Position.x, evt.Position.y, evt.Position.z);
            _eventSource.transform.position = pos;

            AudioClip clip;
            float volume;
            if (evt.ThresholdIndex >= 3)
            {
                clip = _config != null ? _config.ExplosionClip : null;
                if (clip == null) clip = ProceduralAudioGenerator.Explosion;
                volume = _config != null ? _config.ExplosionVolume : 0.8f;
            }
            else
            {
                clip = _config != null ? _config.CrumbleRumbleClip : null;
                if (clip == null) clip = ProceduralAudioGenerator.CrumbleRumble;
                volume = _config != null ? _config.CrumbleRumbleVolume : 0.7f;
            }

            _eventSource.PlayOneShot(clip, volume);
        }

        private void OnChunkCollected(OreChunkCollectedEvent evt)
        {
            var pos = new Vector3(evt.Position.x, evt.Position.y, evt.Position.z);
            _eventSource.transform.position = pos;

            var clip = _config != null ? _config.CollectionClinkClip : null;
            if (clip == null) clip = ProceduralAudioGenerator.CollectionClink;
            float volume = _config != null ? _config.CollectionClinkVolume : 0.3f;

            _eventSource.PlayOneShot(clip, volume);
        }

        private void LateUpdate()
        {
            if (_stateStore == null) return;

            // Pitch ramp from depletion
            if (_isMining && _humSource.isPlaying)
            {
                var mining = _stateStore.Current.Loop.Mining;
                float pitchMin = _config != null ? _config.LaserHumPitchMin : 0.8f;
                float pitchMax = _config != null ? _config.LaserHumPitchMax : 1.4f;
                _humSource.pitch = MiningVFXFormulas.CalculateHumPitch(mining.DepletionFraction, pitchMin, pitchMax);
            }

            // Fade-out on stop
            if (_isFadingOut)
            {
                float duration = _config != null ? _config.LaserHumFadeOutDuration : 0.3f;
                _fadeElapsed += Time.deltaTime;
                float vol = MiningVFXFormulas.CalculateFadeVolume(_fadeElapsed, duration);
                _humSource.volume = vol * (_config != null ? _config.LaserHumBaseVolume : 0.6f);

                if (vol <= 0f)
                {
                    _humSource.Stop();
                    _isFadingOut = false;
                }
            }
        }
    }
}
