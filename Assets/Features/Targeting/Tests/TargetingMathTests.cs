using NUnit.Framework;
using UnityEngine;
using VoidHarvest.Features.Targeting.Systems;

namespace VoidHarvest.Features.Targeting.Tests
{
    [TestFixture]
    public class TargetingMathTests
    {
        // --- FormatRange ---

        [Test]
        public void FormatRange_ThousandsSeparator()
        {
            Assert.AreEqual("1,247 m", TargetingMath.FormatRange(1247f));
        }

        [Test]
        public void FormatRange_NoSeparatorBelowThousand()
        {
            Assert.AreEqual("523 m", TargetingMath.FormatRange(523f));
        }

        [Test]
        public void FormatRange_Zero()
        {
            Assert.AreEqual("0 m", TargetingMath.FormatRange(0f));
        }

        // --- IsInViewport ---

        [Test]
        public void IsInViewport_TrueForCenterScreen()
        {
            // Center of a 1920x1080 screen with positive z
            var pos = new Vector3(960f, 540f, 10f);
            Assert.IsTrue(TargetingMath.IsInViewport(pos, 1920f, 1080f));
        }

        [Test]
        public void IsInViewport_FalseForBehindCamera()
        {
            var pos = new Vector3(960f, 540f, -5f);
            Assert.IsFalse(TargetingMath.IsInViewport(pos, 1920f, 1080f));
        }

        [Test]
        public void IsInViewport_FalseForOutsideBounds()
        {
            var pos = new Vector3(-100f, 540f, 10f);
            Assert.IsFalse(TargetingMath.IsInViewport(pos, 1920f, 1080f));
        }

        // --- ClampToScreenEdge ---

        [Test]
        public void ClampToScreenEdge_ClampsAndReturnsAngle()
        {
            // Off-screen to the upper-right
            var screenPos = new Vector2(2000f, 1200f);
            var screenSize = new Vector2(1920f, 1080f);
            float margin = 30f;

            var (position, angle) = TargetingMath.ClampToScreenEdge(screenPos, screenSize, margin);

            // Should be clamped to screen edges minus margin
            Assert.AreEqual(1920f - margin, position.x, 0.01f);
            Assert.AreEqual(1080f - margin, position.y, 0.01f);
            // Angle should point toward the original off-screen position (upper-right)
            Assert.Greater(angle, 0f);
        }
    }
}
