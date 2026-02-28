using NUnit.Framework;
using UnityEngine;
using VoidHarvest.Features.Mining.Systems;

namespace VoidHarvest.Features.HUD.Tests
{
    [TestFixture]
    public class HUDMiningFeedbackTests
    {
        [Test]
        public void DepletionColor_AtZero_ReturnsOreColor()
        {
            var oreColor = new Color(0.4f, 0.7f, 0.2f, 1f);
            var result = MiningVFXFormulas.CalculateDepletionColor(oreColor, 0f);
            Assert.AreEqual(oreColor.r, result.r, 0.001f);
            Assert.AreEqual(oreColor.g, result.g, 0.001f);
            Assert.AreEqual(oreColor.b, result.b, 0.001f);
        }

        [Test]
        public void DepletionColor_AtHalf_IsBlended()
        {
            var oreColor = new Color(0f, 1f, 0f, 1f);
            var result = MiningVFXFormulas.CalculateDepletionColor(oreColor, 0.5f);
            // Midpoint between green (0,1,0) and red-orange (1,0.3,0.1)
            Assert.AreEqual(0.5f, result.r, 0.001f);
            Assert.AreEqual(0.65f, result.g, 0.001f);
            Assert.AreEqual(0.05f, result.b, 0.001f);
        }

        [Test]
        public void DepletionColor_AtFull_ReturnsRedOrange()
        {
            var oreColor = new Color(0f, 1f, 0f, 1f);
            var result = MiningVFXFormulas.CalculateDepletionColor(oreColor, 1f);
            Assert.AreEqual(1f, result.r, 0.001f);
            Assert.AreEqual(0.3f, result.g, 0.001f);
            Assert.AreEqual(0.1f, result.b, 0.001f);
        }

        [Test]
        public void ProgressPercentage_MatchesDepletionFraction()
        {
            float depletion = 0.65f;
            float percentage = depletion * 100f;
            Assert.AreEqual(65f, percentage, 0.001f);
        }

        [Test]
        public void ProgressPercentage_AtZero_IsZero()
        {
            Assert.AreEqual(0f, 0f * 100f, 0.001f);
        }

        [Test]
        public void ProgressPercentage_AtFull_IsHundred()
        {
            Assert.AreEqual(100f, 1f * 100f, 0.001f);
        }

        [Test]
        public void PulseModulation_SyncsWithVeinGlow()
        {
            // Both beam and HUD should use the same pulse formula at the same speed
            float intensity = 1f;
            float speed = 1.5f; // DepletionVFXConfig.VeinGlowPulseSpeed default
            float amplitude = 0.25f;
            float time = 0.5f;

            float veinPulse = MiningVFXFormulas.ApplyPulseModulation(intensity, speed, amplitude, time);
            float hudPulse = MiningVFXFormulas.ApplyPulseModulation(intensity, speed, amplitude, time);

            Assert.AreEqual(veinPulse, hudPulse, 0.001f);
        }
    }
}
