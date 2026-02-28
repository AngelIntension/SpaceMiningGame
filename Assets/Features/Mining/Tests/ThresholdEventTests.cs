using NUnit.Framework;
using Unity.Mathematics;
using VoidHarvest.Features.Mining.Data;
using VoidHarvest.Features.Mining.Systems;

namespace VoidHarvest.Features.Mining.Tests
{
    [TestFixture]
    public class ThresholdEventTests
    {
        [Test]
        public void NativeThresholdCrossedAction_PreservesThresholdIndex()
        {
            var action = new NativeThresholdCrossedAction
            {
                ThresholdIndex = 2,
                Position = new float3(1, 2, 3),
                Radius = 5f
            };

            Assert.AreEqual(2, action.ThresholdIndex);
        }

        [Test]
        public void NativeThresholdCrossedAction_PreservesPosition()
        {
            var pos = new float3(10f, 20f, 30f);
            var action = new NativeThresholdCrossedAction
            {
                ThresholdIndex = 0,
                Position = pos,
                Radius = 1f
            };

            Assert.AreEqual(10f, action.Position.x, 0.001f);
            Assert.AreEqual(20f, action.Position.y, 0.001f);
            Assert.AreEqual(30f, action.Position.z, 0.001f);
        }

        [Test]
        public void NativeThresholdCrossedAction_PreservesRadius()
        {
            var action = new NativeThresholdCrossedAction
            {
                ThresholdIndex = 1,
                Position = float3.zero,
                Radius = 7.5f
            };

            Assert.AreEqual(7.5f, action.Radius, 0.001f);
        }

        [TestCase((byte)0)]
        [TestCase((byte)1)]
        [TestCase((byte)2)]
        [TestCase((byte)3)]
        public void NativeThresholdCrossedAction_AllThresholdIndicesValid(byte index)
        {
            var action = new NativeThresholdCrossedAction
            {
                ThresholdIndex = index,
                Position = float3.zero,
                Radius = 1f
            };

            Assert.AreEqual(index, action.ThresholdIndex);
        }

        [Test]
        public void DetectThresholdCrossing_At25Percent_SetsBit0()
        {
            bool crossed = AsteroidDepletionFormulas.DetectThresholdCrossing(0.25f, 0, out byte newMask);

            Assert.IsTrue(crossed);
            Assert.AreEqual(0x01, newMask & 0x01);
        }

        [Test]
        public void DetectThresholdCrossing_At50Percent_SetsBit1()
        {
            bool crossed = AsteroidDepletionFormulas.DetectThresholdCrossing(0.50f, 0x01, out byte newMask);

            Assert.IsTrue(crossed);
            Assert.AreEqual(0x02, newMask & 0x02);
        }

        [Test]
        public void DetectThresholdCrossing_At75Percent_SetsBit2()
        {
            bool crossed = AsteroidDepletionFormulas.DetectThresholdCrossing(0.75f, 0x03, out byte newMask);

            Assert.IsTrue(crossed);
            Assert.AreEqual(0x04, newMask & 0x04);
        }

        [Test]
        public void DetectThresholdCrossing_At100Percent_SetsBit3()
        {
            bool crossed = AsteroidDepletionFormulas.DetectThresholdCrossing(1.00f, 0x07, out byte newMask);

            Assert.IsTrue(crossed);
            Assert.AreEqual(0x08, newMask & 0x08);
        }

        [Test]
        public void DetectThresholdCrossing_AlreadyCrossed_ReturnsFalse()
        {
            bool crossed = AsteroidDepletionFormulas.DetectThresholdCrossing(0.25f, 0x01, out byte newMask);

            Assert.IsFalse(crossed);
            Assert.AreEqual(0x01, newMask);
        }

        [Test]
        public void DetectThresholdCrossing_Below25_NoCrossing()
        {
            bool crossed = AsteroidDepletionFormulas.DetectThresholdCrossing(0.24f, 0, out byte newMask);

            Assert.IsFalse(crossed);
            Assert.AreEqual(0, newMask);
        }
    }
}
