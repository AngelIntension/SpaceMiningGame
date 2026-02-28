using NUnit.Framework;
using UnityEngine;
using VoidHarvest.Features.Mining.Systems;

namespace VoidHarvest.Features.Mining.Tests
{
    [TestFixture]
    public class MiningBeamVFXTests
    {
        [Test]
        public void BeamPulseWidth_AtZeroTime_ReturnsBaseWidth()
        {
            float baseWidth = 0.15f;
            float amplitude = 0.3f;
            float speed = 3.0f;
            float time = 0f;

            float result = MiningVFXFormulas.CalculatePulseWidth(baseWidth, speed, amplitude, time);

            Assert.AreEqual(baseWidth, result, 0.001f);
        }

        [Test]
        public void BeamPulseWidth_AtQuarterCycle_ReturnsMaxWidth()
        {
            float baseWidth = 0.15f;
            float amplitude = 0.3f;
            float speed = 1.0f;
            float time = 0.25f;

            float result = MiningVFXFormulas.CalculatePulseWidth(baseWidth, speed, amplitude, time);

            float expected = baseWidth * (1f + amplitude);
            Assert.AreEqual(expected, result, 0.001f);
        }

        [Test]
        public void BeamPulseWidth_AtThreeQuarterCycle_ReturnsMinWidth()
        {
            float baseWidth = 0.15f;
            float amplitude = 0.3f;
            float speed = 1.0f;
            float time = 0.75f;

            float result = MiningVFXFormulas.CalculatePulseWidth(baseWidth, speed, amplitude, time);

            float expected = baseWidth * (1f - amplitude);
            Assert.AreEqual(expected, result, 0.001f);
        }

        [Test]
        public void BeamPulseWidth_ZeroAmplitude_ReturnsSteadyWidth()
        {
            float baseWidth = 0.15f;
            float amplitude = 0f;
            float speed = 3.0f;

            for (float t = 0f; t <= 2f; t += 0.1f)
            {
                float result = MiningVFXFormulas.CalculatePulseWidth(baseWidth, speed, amplitude, t);
                Assert.AreEqual(baseWidth, result, 0.001f);
            }
        }

        [Test]
        public void SparkColorResolution_MatchesOreBeamColor()
        {
            var oreColor = new Color(0.2f, 0.8f, 0.3f, 1f);
            Color result = MiningVFXFormulas.ResolveSparkColor(oreColor);
            Assert.AreEqual(oreColor, result);
        }

        [Test]
        public void SparkColorResolution_DefaultColor_WhenNoOre()
        {
            Color result = MiningVFXFormulas.ResolveSparkColor(null);
            Assert.AreEqual(Color.white, result);
        }

        [Test]
        public void HeatShimmerOpacity_MatchesConfig()
        {
            float configIntensity = 0.5f;
            float result = MiningVFXFormulas.CalculateHeatShimmerOpacity(configIntensity);
            Assert.AreEqual(0.5f, result, 0.001f);
        }

        [Test]
        public void HeatShimmerOpacity_ClampedToZeroOne()
        {
            Assert.AreEqual(0f, MiningVFXFormulas.CalculateHeatShimmerOpacity(0f), 0.001f);
            Assert.AreEqual(1f, MiningVFXFormulas.CalculateHeatShimmerOpacity(1f), 0.001f);
        }

        [Test]
        public void CleanShutdownState_AllFlagsReset()
        {
            bool beamActive = true;
            bool sparksActive = true;
            bool shimmerActive = true;

            beamActive = false;
            sparksActive = false;
            shimmerActive = false;

            Assert.IsFalse(beamActive);
            Assert.IsFalse(sparksActive);
            Assert.IsFalse(shimmerActive);
        }
    }
}
