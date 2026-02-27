using NUnit.Framework;
using VoidHarvest.Features.Mining.Systems;

namespace VoidHarvest.Features.Mining.Tests
{
    /// <summary>
    /// EditMode tests for fade-out timing and entity destruction logic.
    /// See FR-021: Fade-out removal, SC-011: Removal timing.
    /// </summary>
    [TestFixture]
    public class AsteroidDestroyTests
    {
        // --- T012: Fade-out timing tests ---

        [Test]
        public void CalculateFadeAlpha_FullTimer_ReturnsOne()
        {
            float alpha = AsteroidDepletionFormulas.CalculateFadeAlpha(0.5f, 0.5f);

            Assert.AreEqual(1f, alpha, 0.001f,
                "Full timer (equal to duration) should produce alpha 1.0");
        }

        [Test]
        public void CalculateFadeAlpha_HalfTimer_ReturnsHalf()
        {
            float alpha = AsteroidDepletionFormulas.CalculateFadeAlpha(0.25f, 0.5f);

            Assert.AreEqual(0.5f, alpha, 0.001f,
                "Half-remaining timer should produce alpha 0.5");
        }

        [Test]
        public void CalculateFadeAlpha_ZeroTimer_ReturnsZero()
        {
            float alpha = AsteroidDepletionFormulas.CalculateFadeAlpha(0f, 0.5f);

            Assert.AreEqual(0f, alpha, 0.001f,
                "Zero timer should produce alpha 0.0");
        }

        [Test]
        public void CalculateFadeAlpha_NegativeTimer_ClampedToZero()
        {
            float alpha = AsteroidDepletionFormulas.CalculateFadeAlpha(-0.1f, 0.5f);

            Assert.AreEqual(0f, alpha, 0.001f,
                "Negative timer should clamp alpha to 0.0");
        }

        [Test]
        public void CalculateFadeAlpha_TimerExceedsDuration_ClampedToOne()
        {
            float alpha = AsteroidDepletionFormulas.CalculateFadeAlpha(1.0f, 0.5f);

            Assert.AreEqual(1f, alpha, 0.001f,
                "Timer exceeding duration should clamp alpha to 1.0");
        }

        [Test]
        public void CalculateFadeAlpha_ZeroDuration_ReturnsZero()
        {
            float alpha = AsteroidDepletionFormulas.CalculateFadeAlpha(0.5f, 0f);

            Assert.AreEqual(0f, alpha, 0.001f,
                "Zero fade duration should produce alpha 0.0 (instant destruction)");
        }

        [Test]
        public void CalculateFadeAlpha_CountdownSequence_InterpolatesCorrectly()
        {
            const float duration = 0.5f;

            // Simulate countdown: 0.5 → 0.4 → 0.3 → 0.2 → 0.1 → 0.0
            float[] timers = { 0.5f, 0.4f, 0.3f, 0.2f, 0.1f, 0.0f };
            float[] expectedAlphas = { 1.0f, 0.8f, 0.6f, 0.4f, 0.2f, 0.0f };

            for (int i = 0; i < timers.Length; i++)
            {
                float alpha = AsteroidDepletionFormulas.CalculateFadeAlpha(timers[i], duration);
                Assert.AreEqual(expectedAlphas[i], alpha, 0.001f,
                    $"Timer {timers[i]} should produce alpha {expectedAlphas[i]}");
            }
        }

        // --- ShouldDestroy tests ---

        [Test]
        public void ShouldDestroy_PositiveTimer_ReturnsFalse()
        {
            Assert.IsFalse(AsteroidDepletionFormulas.ShouldDestroy(0.1f),
                "Positive timer means entity still fading — not yet destroyed");
        }

        [Test]
        public void ShouldDestroy_ZeroTimer_ReturnsFalse()
        {
            Assert.IsFalse(AsteroidDepletionFormulas.ShouldDestroy(0f),
                "Zero timer means fade just completed — not yet past threshold");
        }

        [Test]
        public void ShouldDestroy_NegativeTimer_ReturnsTrue()
        {
            Assert.IsTrue(AsteroidDepletionFormulas.ShouldDestroy(-0.01f),
                "Negative timer means fade expired — entity should be destroyed");
        }

        [Test]
        public void ShouldDestroy_LargeNegative_ReturnsTrue()
        {
            Assert.IsTrue(AsteroidDepletionFormulas.ShouldDestroy(-100f),
                "Any negative timer should mark entity for destruction");
        }

        // --- Constants ---

        [Test]
        public void CrumblePauseDuration_IsHalfSecond()
        {
            Assert.AreEqual(0.5f, AsteroidDepletionFormulas.CrumblePauseDuration, 0.001f,
                "Crumble pause duration should be 0.5 seconds");
        }

        [Test]
        public void FadeOutDuration_IsHalfSecond()
        {
            Assert.AreEqual(0.5f, AsteroidDepletionFormulas.FadeOutDuration, 0.001f,
                "Fade-out duration should be 0.5 seconds");
        }
    }
}
