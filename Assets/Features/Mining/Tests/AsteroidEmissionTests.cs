using NUnit.Framework;
using UnityEngine;
using VoidHarvest.Features.Mining.Systems;

namespace VoidHarvest.Features.Mining.Tests
{
    [TestFixture]
    public class AsteroidEmissionTests
    {
        [Test]
        public void EmissionIntensity_AtZeroDepletion_ReturnsMin()
        {
            float result = MiningVFXFormulas.CalculateEmissionIntensity(0f, 0.1f, 3.0f);
            Assert.AreEqual(0.1f, result, 0.001f);
        }

        [Test]
        public void EmissionIntensity_AtFullDepletion_ReturnsMax()
        {
            float result = MiningVFXFormulas.CalculateEmissionIntensity(1.0f, 0.1f, 3.0f);
            Assert.AreEqual(3.0f, result, 0.001f);
        }

        [Test]
        public void EmissionIntensity_AtHalfDepletion_UsesSqrtCurve()
        {
            float result = MiningVFXFormulas.CalculateEmissionIntensity(0.5f, 0.1f, 3.0f);
            // sqrt(0.5) = 0.7071, lerp(0.1, 3.0, 0.7071) = 0.1 + 2.9 * 0.7071 = 2.1506
            float expected = Mathf.Lerp(0.1f, 3.0f, Mathf.Sqrt(0.5f));
            Assert.AreEqual(expected, result, 0.001f);
        }

        [Test]
        public void EmissionIntensity_SqrtCurve_IsMonotonicallyIncreasing()
        {
            float prev = MiningVFXFormulas.CalculateEmissionIntensity(0f, 0.1f, 3.0f);
            for (float d = 0.1f; d <= 1f; d += 0.1f)
            {
                float current = MiningVFXFormulas.CalculateEmissionIntensity(d, 0.1f, 3.0f);
                Assert.Greater(current, prev, $"Intensity should increase at depletion {d}");
                prev = current;
            }
        }

        [Test]
        public void PulseModulation_AtZeroTime_ReturnsBaseIntensity()
        {
            float intensity = 2.0f;
            float result = MiningVFXFormulas.ApplyPulseModulation(intensity, 1.5f, 0.25f, 0f);
            // sin(0) = 0, so modulated = 2.0 * (1 + 0 * 0.25) = 2.0
            Assert.AreEqual(2.0f, result, 0.001f);
        }

        [Test]
        public void PulseModulation_AtQuarterCycle_ReturnsMaxIntensity()
        {
            float intensity = 2.0f;
            float speed = 1.0f;
            float amplitude = 0.25f;
            float time = 0.25f; // sin(PI/2) = 1

            float result = MiningVFXFormulas.ApplyPulseModulation(intensity, speed, amplitude, time);
            float expected = intensity * (1f + amplitude);
            Assert.AreEqual(expected, result, 0.001f);
        }

        [Test]
        public void PulseModulation_ZeroAmplitude_ReturnsSteady()
        {
            float intensity = 1.5f;
            for (float t = 0f; t <= 2f; t += 0.1f)
            {
                float result = MiningVFXFormulas.ApplyPulseModulation(intensity, 1.5f, 0f, t);
                Assert.AreEqual(intensity, result, 0.001f);
            }
        }

        [Test]
        public void EmissionIntensity_NegativeDepletion_ClampedToZero()
        {
            float result = MiningVFXFormulas.CalculateEmissionIntensity(-0.5f, 0.1f, 3.0f);
            Assert.AreEqual(0.1f, result, 0.001f);
        }

        [Test]
        public void EmissionIntensity_OverOneDepletion_ClampedToOne()
        {
            float result = MiningVFXFormulas.CalculateEmissionIntensity(1.5f, 0.1f, 3.0f);
            Assert.AreEqual(3.0f, result, 0.001f);
        }
    }
}
