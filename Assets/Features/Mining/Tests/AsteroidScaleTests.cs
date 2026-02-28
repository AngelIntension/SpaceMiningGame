using NUnit.Framework;
using VoidHarvest.Features.Mining.Systems;

namespace VoidHarvest.Features.Mining.Tests
{
    /// <summary>
    /// EditMode tests for asteroid scale formula and crumble threshold detection.
    /// See FR-019: Depletion shrink, FR-020: Crumble pauses.
    /// </summary>
    [TestFixture]
    public class AsteroidScaleTests
    {
        private const float Radius = 4f;
        private const float InitialMass = 640f; // 4^3 * 10
        private const float MinScale = 0.3f;

        // --- T010: Scale formula tests ---
        // Formula: scale = radius * lerp(minScaleFraction, 1.0, remainingMass / initialMass)

        [Test]
        public void CalculateScale_FullMass_ReturnsRadius()
        {
            float scale = AsteroidDepletionFormulas.CalculateScale(Radius, InitialMass, InitialMass, MinScale);

            Assert.AreEqual(Radius * 1.0f, scale, 0.001f,
                "At 100% remaining mass, scale should equal radius");
        }

        [Test]
        public void CalculateScale_75PercentRemaining_ReturnsCorrectScale()
        {
            float remaining = InitialMass * 0.75f;
            // lerp(0.3, 1.0, 0.75) = 0.3 + 0.7 * 0.75 = 0.3 + 0.525 = 0.825
            float expected = Radius * 0.825f;

            float scale = AsteroidDepletionFormulas.CalculateScale(Radius, remaining, InitialMass, MinScale);

            Assert.AreEqual(expected, scale, 0.001f,
                "At 75% remaining, scale = radius * 0.825");
        }

        [Test]
        public void CalculateScale_50PercentRemaining_ReturnsCorrectScale()
        {
            float remaining = InitialMass * 0.5f;
            // lerp(0.3, 1.0, 0.5) = 0.3 + 0.7 * 0.5 = 0.65
            float expected = Radius * 0.65f;

            float scale = AsteroidDepletionFormulas.CalculateScale(Radius, remaining, InitialMass, MinScale);

            Assert.AreEqual(expected, scale, 0.001f,
                "At 50% remaining, scale = radius * 0.65");
        }

        [Test]
        public void CalculateScale_25PercentRemaining_ReturnsCorrectScale()
        {
            float remaining = InitialMass * 0.25f;
            // lerp(0.3, 1.0, 0.25) = 0.3 + 0.7 * 0.25 = 0.475
            float expected = Radius * 0.475f;

            float scale = AsteroidDepletionFormulas.CalculateScale(Radius, remaining, InitialMass, MinScale);

            Assert.AreEqual(expected, scale, 0.001f,
                "At 25% remaining, scale = radius * 0.475");
        }

        [Test]
        public void CalculateScale_ZeroRemaining_ReturnsMinScale()
        {
            // lerp(0.3, 1.0, 0) = 0.3
            float expected = Radius * MinScale;

            float scale = AsteroidDepletionFormulas.CalculateScale(Radius, 0f, InitialMass, MinScale);

            Assert.AreEqual(expected, scale, 0.001f,
                "At 0% remaining, scale = radius * minScaleFraction");
        }

        [Test]
        public void CalculateScale_ZeroInitialMass_ReturnsMinScale()
        {
            // Prevent division by zero: when initialMass is 0, fraction = 0
            float expected = Radius * MinScale;

            float scale = AsteroidDepletionFormulas.CalculateScale(Radius, 0f, 0f, MinScale);

            Assert.AreEqual(expected, scale, 0.001f,
                "Zero initial mass should produce min scale (no division by zero)");
        }

        [Test]
        public void CalculateScale_NegativeRemaining_ClampedToMinScale()
        {
            // Remaining can't go below 0 logically, but formula should handle it gracefully
            float expected = Radius * MinScale;

            float scale = AsteroidDepletionFormulas.CalculateScale(Radius, -100f, InitialMass, MinScale);

            Assert.AreEqual(expected, scale, 0.001f,
                "Negative remaining mass should clamp to min scale");
        }

        // --- T011: Crumble threshold detection tests ---

        [Test]
        public void DetectThreshold_FreshAsteroid_NoCrossing()
        {
            bool crossed = AsteroidDepletionFormulas.DetectThresholdCrossing(0f, 0x00, out byte newMask);

            Assert.IsFalse(crossed, "0% depletion should not cross any threshold");
            Assert.AreEqual(0x00, newMask, "Mask should remain 0");
        }

        [Test]
        public void DetectThreshold_Cross25Percent_SetsBit0()
        {
            bool crossed = AsteroidDepletionFormulas.DetectThresholdCrossing(0.25f, 0x00, out byte newMask);

            Assert.IsTrue(crossed, "25% depletion should cross first threshold");
            Assert.AreEqual(0x01, newMask, "bit0 should be set");
        }

        [Test]
        public void DetectThreshold_AlreadyPast25_NoRetrigger()
        {
            // Already passed 25% threshold (bit0 set), depletion still at 30%
            bool crossed = AsteroidDepletionFormulas.DetectThresholdCrossing(0.30f, 0x01, out byte newMask);

            Assert.IsFalse(crossed, "Already-passed threshold should not re-trigger");
            Assert.AreEqual(0x01, newMask, "Mask should remain unchanged");
        }

        [Test]
        public void DetectThreshold_Cross50Percent_SetsBit1()
        {
            // Already past 25% (bit0), now crossing 50%
            bool crossed = AsteroidDepletionFormulas.DetectThresholdCrossing(0.50f, 0x01, out byte newMask);

            Assert.IsTrue(crossed, "50% depletion should cross second threshold");
            Assert.AreEqual(0x03, newMask, "bit0 and bit1 should be set");
        }

        [Test]
        public void DetectThreshold_Cross75Percent_SetsBit2()
        {
            bool crossed = AsteroidDepletionFormulas.DetectThresholdCrossing(0.75f, 0x03, out byte newMask);

            Assert.IsTrue(crossed, "75% depletion should cross third threshold");
            Assert.AreEqual(0x07, newMask, "bit0, bit1, and bit2 should be set");
        }

        [Test]
        public void DetectThreshold_FullDepletion_SetsBit3()
        {
            bool crossed = AsteroidDepletionFormulas.DetectThresholdCrossing(1.0f, 0x07, out byte newMask);

            Assert.IsTrue(crossed, "100% depletion should cross final threshold");
            Assert.AreEqual(0x0F, newMask, "All 4 bits should be set");
        }

        [Test]
        public void DetectThreshold_JumpToFull_SetsAllBits()
        {
            // Jump from 0% to 100% at once (e.g., very high mining power)
            bool crossed = AsteroidDepletionFormulas.DetectThresholdCrossing(1.0f, 0x00, out byte newMask);

            Assert.IsTrue(crossed, "Jumping to full depletion should cross all thresholds");
            Assert.AreEqual(0x0F, newMask, "All 4 bits should be set");
        }

        [Test]
        public void DetectThreshold_AllAlreadyPassed_NoCrossing()
        {
            bool crossed = AsteroidDepletionFormulas.DetectThresholdCrossing(1.0f, 0x0F, out byte newMask);

            Assert.IsFalse(crossed, "All thresholds already passed should not trigger");
            Assert.AreEqual(0x0F, newMask, "Mask should remain all bits set");
        }

        [Test]
        public void DetectThreshold_JustBelow25_NoCrossing()
        {
            bool crossed = AsteroidDepletionFormulas.DetectThresholdCrossing(0.24f, 0x00, out byte newMask);

            Assert.IsFalse(crossed, "Just below 25% should not cross threshold");
            Assert.AreEqual(0x00, newMask, "Mask should remain 0");
        }
    }
}
