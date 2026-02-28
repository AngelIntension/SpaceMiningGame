using NUnit.Framework;
using VoidHarvest.Features.Mining.Systems;

namespace VoidHarvest.Features.Mining.Tests
{
    [TestFixture]
    public class MiningAudioTests
    {
        [Test]
        public void HumPitch_AtZeroDepletion_ReturnsPitchMin()
        {
            float pitchMin = 0.8f;
            float pitchMax = 1.4f;

            float result = MiningVFXFormulas.CalculateHumPitch(0f, pitchMin, pitchMax);

            Assert.AreEqual(pitchMin, result, 0.001f);
        }

        [Test]
        public void HumPitch_AtHalfDepletion_ReturnsMidpoint()
        {
            float pitchMin = 0.8f;
            float pitchMax = 1.4f;

            float result = MiningVFXFormulas.CalculateHumPitch(0.5f, pitchMin, pitchMax);

            Assert.AreEqual(1.1f, result, 0.001f);
        }

        [Test]
        public void HumPitch_AtFullDepletion_ReturnsPitchMax()
        {
            float pitchMin = 0.8f;
            float pitchMax = 1.4f;

            float result = MiningVFXFormulas.CalculateHumPitch(1f, pitchMin, pitchMax);

            Assert.AreEqual(pitchMax, result, 0.001f);
        }

        [Test]
        public void FadeVolume_AtStart_ReturnsFullVolume()
        {
            float result = MiningVFXFormulas.CalculateFadeVolume(0f, 0.3f);
            Assert.AreEqual(1f, result, 0.001f);
        }

        [Test]
        public void FadeVolume_AtHalfDuration_ReturnsHalf()
        {
            float result = MiningVFXFormulas.CalculateFadeVolume(0.15f, 0.3f);
            Assert.AreEqual(0.5f, result, 0.001f);
        }

        [Test]
        public void FadeVolume_AtEnd_ReturnsZero()
        {
            float result = MiningVFXFormulas.CalculateFadeVolume(0.3f, 0.3f);
            Assert.AreEqual(0f, result, 0.001f);
        }

        [Test]
        public void FadeVolume_BeyondEnd_ClampedToZero()
        {
            float result = MiningVFXFormulas.CalculateFadeVolume(0.5f, 0.3f);
            Assert.AreEqual(0f, result, 0.001f);
        }

        [Test]
        public void FadeVolume_ZeroDuration_ReturnsZero()
        {
            float result = MiningVFXFormulas.CalculateFadeVolume(0f, 0f);
            Assert.AreEqual(0f, result, 0.001f);
        }

        [Test]
        public void HumPitch_ClampedAboveOne()
        {
            float pitchMin = 0.8f;
            float pitchMax = 1.4f;

            float result = MiningVFXFormulas.CalculateHumPitch(1.5f, pitchMin, pitchMax);

            Assert.AreEqual(pitchMax, result, 0.001f);
        }

        [Test]
        public void HumPitch_ClampedBelowZero()
        {
            float pitchMin = 0.8f;
            float pitchMax = 1.4f;

            float result = MiningVFXFormulas.CalculateHumPitch(-0.5f, pitchMin, pitchMax);

            Assert.AreEqual(pitchMin, result, 0.001f);
        }
    }
}
