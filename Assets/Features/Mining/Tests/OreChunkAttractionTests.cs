using NUnit.Framework;
using UnityEngine;
using VoidHarvest.Features.Mining.Systems;

namespace VoidHarvest.Features.Mining.Tests
{
    [TestFixture]
    public class OreChunkAttractionTests
    {
        [Test]
        public void BezierCurve_AtStart_ReturnsP0()
        {
            var p0 = new Vector3(0, 0, 0);
            var p1 = new Vector3(5, 5, 0);
            var p2 = new Vector3(10, 0, 0);

            var result = MiningVFXFormulas.EvaluateBezier(p0, p1, p2, 0f);

            Assert.AreEqual(p0.x, result.x, 0.001f);
            Assert.AreEqual(p0.y, result.y, 0.001f);
            Assert.AreEqual(p0.z, result.z, 0.001f);
        }

        [Test]
        public void BezierCurve_AtEnd_ReturnsP2()
        {
            var p0 = new Vector3(0, 0, 0);
            var p1 = new Vector3(5, 5, 0);
            var p2 = new Vector3(10, 0, 0);

            var result = MiningVFXFormulas.EvaluateBezier(p0, p1, p2, 1f);

            Assert.AreEqual(p2.x, result.x, 0.001f);
            Assert.AreEqual(p2.y, result.y, 0.001f);
            Assert.AreEqual(p2.z, result.z, 0.001f);
        }

        [Test]
        public void BezierCurve_AtMidpoint_IsNotLinear()
        {
            var p0 = new Vector3(0, 0, 0);
            var p1 = new Vector3(5, 10, 0);
            var p2 = new Vector3(10, 0, 0);

            var result = MiningVFXFormulas.EvaluateBezier(p0, p1, p2, 0.5f);

            // Linear midpoint would be (5, 0, 0). Bezier should be above that.
            Assert.AreEqual(5f, result.x, 0.001f);
            Assert.Greater(result.y, 0f, "Bezier midpoint should deviate from linear path");
        }

        [Test]
        public void BezierCurve_IsMonotonicallyProgressingInX()
        {
            var p0 = new Vector3(0, 0, 0);
            var p1 = new Vector3(5, 5, 0);
            var p2 = new Vector3(10, 0, 0);

            float prevX = -1f;
            for (float t = 0f; t <= 1f; t += 0.1f)
            {
                var pos = MiningVFXFormulas.EvaluateBezier(p0, p1, p2, t);
                Assert.Greater(pos.x, prevX, $"X should increase monotonically at t={t}");
                prevX = pos.x;
            }
        }

        [Test]
        public void BezierCurve_ReachesTarget_AtT1()
        {
            var asteroid = new Vector3(100, 50, 30);
            var control = new Vector3(80, 60, 20);
            var barge = new Vector3(0, 0, 0);

            var result = MiningVFXFormulas.EvaluateBezier(asteroid, control, barge, 1f);

            Assert.AreEqual(barge.x, result.x, 0.001f);
            Assert.AreEqual(barge.y, result.y, 0.001f);
            Assert.AreEqual(barge.z, result.z, 0.001f);
        }

        [Test]
        public void DepletionColor_AtZero_ReturnsOreColor()
        {
            var oreColor = new Color(0.3f, 0.8f, 0.2f, 1f);
            var result = MiningVFXFormulas.CalculateDepletionColor(oreColor, 0f);
            Assert.AreEqual(oreColor.r, result.r, 0.001f);
            Assert.AreEqual(oreColor.g, result.g, 0.001f);
            Assert.AreEqual(oreColor.b, result.b, 0.001f);
        }

        [Test]
        public void DepletionColor_AtFull_ReturnsRedOrange()
        {
            var oreColor = new Color(0.3f, 0.8f, 0.2f, 1f);
            var result = MiningVFXFormulas.CalculateDepletionColor(oreColor, 1f);
            // Should be shifted toward red-orange (1, 0.3, 0.1)
            Assert.AreEqual(1f, result.r, 0.001f);
            Assert.AreEqual(0.3f, result.g, 0.001f);
            Assert.AreEqual(0.1f, result.b, 0.001f);
        }
    }
}
