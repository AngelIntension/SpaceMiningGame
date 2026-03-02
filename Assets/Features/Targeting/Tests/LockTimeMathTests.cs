using NUnit.Framework;
using VoidHarvest.Core.Extensions;
using VoidHarvest.Features.Targeting.Systems;

namespace VoidHarvest.Features.Targeting.Tests
{
    [TestFixture]
    public class LockTimeMathTests
    {
        [Test]
        public void CalculateLockTime_ReturnsBaseLockTime()
        {
            var target = TargetInfo.FromAsteroid(1, "Asteroid", "Luminite");
            float result = LockTimeMath.CalculateLockTime(1.5f, target);
            Assert.AreEqual(1.5f, result, 0.001f);
        }

        [Test]
        public void CalculateLockTime_AcceptsTargetInfoParameter()
        {
            var target = new TargetInfo(42, "Station", "Station", TargetType.Station);
            float result = LockTimeMath.CalculateLockTime(2.0f, target);
            Assert.AreEqual(2.0f, result, 0.001f);
        }

        [Test]
        public void CalculateLockTime_ReturnsBaseLockTimeForNone()
        {
            float result = LockTimeMath.CalculateLockTime(3.0f, TargetInfo.None);
            Assert.AreEqual(3.0f, result, 0.001f);
        }

        [Test]
        public void CalculateLockTime_ReturnsBaseLockTimeForAsteroid()
        {
            var target = TargetInfo.FromAsteroid(5, "Rock #5", "Ferrox");
            float result = LockTimeMath.CalculateLockTime(1.5f, target);
            Assert.AreEqual(1.5f, result, 0.001f);
        }

        [Test]
        public void CalculateLockTime_ReturnsBaseLockTimeForStation()
        {
            var target = new TargetInfo(99, "Medium Refinery Hub", "Station", TargetType.Station);
            float result = LockTimeMath.CalculateLockTime(2.5f, target);
            Assert.AreEqual(2.5f, result, 0.001f);
        }
    }
}
