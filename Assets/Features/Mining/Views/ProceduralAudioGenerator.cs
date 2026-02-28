using UnityEngine;

namespace VoidHarvest.Features.Mining.Views
{
    /// <summary>
    /// Generates placeholder AudioClips procedurally when no designer audio is assigned.
    /// Clips are cached on first access for zero-allocation reuse.
    /// </summary>
    public static class ProceduralAudioGenerator
    {
        private static AudioClip _laserHum;
        private static AudioClip _sparkCrackle;
        private static AudioClip _crumbleRumble;
        private static AudioClip _explosion;
        private static AudioClip _collectionClink;

        private const int SampleRate = 44100;

        /// <summary>80Hz sine + harmonics, looping, 1 second.</summary>
        public static AudioClip LaserHum
        {
            get
            {
                if (_laserHum == null)
                    _laserHum = CreateLaserHum();
                return _laserHum;
            }
        }

        /// <summary>White noise burst, 0.1s.</summary>
        public static AudioClip SparkCrackle
        {
            get
            {
                if (_sparkCrackle == null)
                    _sparkCrackle = CreateSparkCrackle();
                return _sparkCrackle;
            }
        }

        /// <summary>40-60Hz sine sweep, 0.5s.</summary>
        public static AudioClip CrumbleRumble
        {
            get
            {
                if (_crumbleRumble == null)
                    _crumbleRumble = CreateCrumbleRumble();
                return _crumbleRumble;
            }
        }

        /// <summary>White noise + low sine, 0.8s, amplitude envelope.</summary>
        public static AudioClip Explosion
        {
            get
            {
                if (_explosion == null)
                    _explosion = CreateExplosion();
                return _explosion;
            }
        }

        /// <summary>2kHz sine ping, 0.1s, fast decay.</summary>
        public static AudioClip CollectionClink
        {
            get
            {
                if (_collectionClink == null)
                    _collectionClink = CreateCollectionClink();
                return _collectionClink;
            }
        }

        private static AudioClip CreateLaserHum()
        {
            int samples = SampleRate; // 1 second
            var data = new float[samples];
            for (int i = 0; i < samples; i++)
            {
                float t = (float)i / SampleRate;
                // 80Hz fundamental + 160Hz harmonic + 240Hz harmonic
                data[i] = Mathf.Sin(2f * Mathf.PI * 80f * t) * 0.5f
                        + Mathf.Sin(2f * Mathf.PI * 160f * t) * 0.3f
                        + Mathf.Sin(2f * Mathf.PI * 240f * t) * 0.1f;
            }
            var clip = AudioClip.Create("ProceduralLaserHum", samples, 1, SampleRate, false);
            clip.SetData(data, 0);
            return clip;
        }

        private static AudioClip CreateSparkCrackle()
        {
            int samples = SampleRate / 10; // 0.1s
            var data = new float[samples];
            var rng = new System.Random(42);
            for (int i = 0; i < samples; i++)
            {
                float envelope = 1f - (float)i / samples;
                data[i] = ((float)rng.NextDouble() * 2f - 1f) * envelope * 0.6f;
            }
            var clip = AudioClip.Create("ProceduralSparkCrackle", samples, 1, SampleRate, false);
            clip.SetData(data, 0);
            return clip;
        }

        private static AudioClip CreateCrumbleRumble()
        {
            int samples = SampleRate / 2; // 0.5s
            var data = new float[samples];
            for (int i = 0; i < samples; i++)
            {
                float t = (float)i / SampleRate;
                float envelope = 1f - (float)i / samples;
                // Sweep from 40Hz to 60Hz
                float freq = Mathf.Lerp(40f, 60f, (float)i / samples);
                data[i] = Mathf.Sin(2f * Mathf.PI * freq * t) * envelope * 0.7f;
            }
            var clip = AudioClip.Create("ProceduralCrumbleRumble", samples, 1, SampleRate, false);
            clip.SetData(data, 0);
            return clip;
        }

        private static AudioClip CreateExplosion()
        {
            int samples = (int)(SampleRate * 0.8f); // 0.8s
            var data = new float[samples];
            var rng = new System.Random(123);
            for (int i = 0; i < samples; i++)
            {
                float t = (float)i / SampleRate;
                float envelope = Mathf.Exp(-3f * (float)i / samples);
                float noise = ((float)rng.NextDouble() * 2f - 1f) * 0.5f;
                float lowSine = Mathf.Sin(2f * Mathf.PI * 50f * t) * 0.5f;
                data[i] = (noise + lowSine) * envelope;
            }
            var clip = AudioClip.Create("ProceduralExplosion", samples, 1, SampleRate, false);
            clip.SetData(data, 0);
            return clip;
        }

        private static AudioClip CreateCollectionClink()
        {
            int samples = SampleRate / 10; // 0.1s
            var data = new float[samples];
            for (int i = 0; i < samples; i++)
            {
                float t = (float)i / SampleRate;
                float envelope = Mathf.Exp(-20f * (float)i / samples);
                data[i] = Mathf.Sin(2f * Mathf.PI * 2000f * t) * envelope * 0.5f;
            }
            var clip = AudioClip.Create("ProceduralCollectionClink", samples, 1, SampleRate, false);
            clip.SetData(data, 0);
            return clip;
        }
    }
}
