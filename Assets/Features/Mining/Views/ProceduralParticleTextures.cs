using UnityEngine;

namespace VoidHarvest.Features.Mining.Views
{
    /// <summary>
    /// Generates procedural particle textures at runtime.
    /// Cached on first access for zero-allocation reuse.
    /// </summary>
    public static class ProceduralParticleTextures
    {
        private static Texture2D _softCircle;
        private static Texture2D _spark;

        private const int CircleSize = 64;
        private const int SparkSize = 64;

        /// <summary>Soft radial gradient circle — general purpose particle.</summary>
        public static Texture2D SoftCircle
        {
            get
            {
                if (_softCircle == null)
                    _softCircle = CreateSoftCircle();
                return _softCircle;
            }
        }

        /// <summary>Elongated spark/streak — for impact sparks.</summary>
        public static Texture2D Spark
        {
            get
            {
                if (_spark == null)
                    _spark = CreateSpark();
                return _spark;
            }
        }

        private static Texture2D CreateSoftCircle()
        {
            var tex = new Texture2D(CircleSize, CircleSize, TextureFormat.RGBA32, false)
            {
                wrapMode = TextureWrapMode.Clamp,
                filterMode = FilterMode.Bilinear
            };

            float center = CircleSize * 0.5f;
            float maxRadius = center;
            var pixels = new Color[CircleSize * CircleSize];

            for (int y = 0; y < CircleSize; y++)
            {
                for (int x = 0; x < CircleSize; x++)
                {
                    float dx = x - center + 0.5f;
                    float dy = y - center + 0.5f;
                    float dist = Mathf.Sqrt(dx * dx + dy * dy) / maxRadius;

                    // Smooth falloff: bright center, soft fade to edge
                    float alpha = Mathf.Clamp01(1f - dist);
                    alpha *= alpha; // Quadratic falloff for softer look

                    pixels[y * CircleSize + x] = new Color(1f, 1f, 1f, alpha);
                }
            }

            tex.SetPixels(pixels);
            tex.Apply(false, true); // makeNoLongerReadable for GPU optimization
            return tex;
        }

        private static Texture2D CreateSpark()
        {
            var tex = new Texture2D(SparkSize, SparkSize, TextureFormat.RGBA32, false)
            {
                wrapMode = TextureWrapMode.Clamp,
                filterMode = FilterMode.Bilinear
            };

            float centerX = SparkSize * 0.5f;
            float centerY = SparkSize * 0.5f;
            var pixels = new Color[SparkSize * SparkSize];

            for (int y = 0; y < SparkSize; y++)
            {
                for (int x = 0; x < SparkSize; x++)
                {
                    float dx = (x - centerX + 0.5f) / centerX;
                    float dy = (y - centerY + 0.5f) / centerY;

                    // Elongated horizontally: tighter vertical falloff
                    float distSq = dx * dx + dy * dy * 4f;
                    float alpha = Mathf.Clamp01(1f - Mathf.Sqrt(distSq));
                    alpha *= alpha;

                    // Add bright core
                    float core = Mathf.Exp(-distSq * 8f);
                    alpha = Mathf.Clamp01(alpha + core);

                    pixels[y * SparkSize + x] = new Color(1f, 1f, 1f, alpha);
                }
            }

            tex.SetPixels(pixels);
            tex.Apply(false, true);
            return tex;
        }
    }
}
