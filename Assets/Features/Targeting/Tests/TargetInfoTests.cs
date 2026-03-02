using NUnit.Framework;
using VoidHarvest.Core.Extensions;

namespace VoidHarvest.Features.Targeting.Tests
{
    [TestFixture]
    public class TargetInfoTests
    {
        private class MockTargetable : ITargetable
        {
            public int TargetId { get; set; }
            public string DisplayName { get; set; }
            public string TypeLabel { get; set; }
            public TargetType TargetType { get; set; }
        }

        [Test]
        public void None_HasNegativeTargetId()
        {
            Assert.AreEqual(-1, TargetInfo.None.TargetId);
        }

        [Test]
        public void None_IsNotValid()
        {
            Assert.IsFalse(TargetInfo.None.IsValid);
        }

        [Test]
        public void None_HasEmptyStringsNotNull()
        {
            Assert.AreEqual(string.Empty, TargetInfo.None.DisplayName);
            Assert.AreEqual(string.Empty, TargetInfo.None.TypeLabel);
        }

        [Test]
        public void None_HasTargetTypeNone()
        {
            Assert.AreEqual(TargetType.None, TargetInfo.None.TargetType);
        }

        [Test]
        public void From_CopiesAllFields()
        {
            var mock = new MockTargetable
            {
                TargetId = 42,
                DisplayName = "Test Station",
                TypeLabel = "Station",
                TargetType = TargetType.Station
            };

            var info = TargetInfo.From(mock);

            Assert.AreEqual(42, info.TargetId);
            Assert.AreEqual("Test Station", info.DisplayName);
            Assert.AreEqual("Station", info.TypeLabel);
            Assert.AreEqual(TargetType.Station, info.TargetType);
        }

        [Test]
        public void From_NullTarget_ReturnsNone()
        {
            var info = TargetInfo.From(null);

            Assert.AreEqual(-1, info.TargetId);
            Assert.IsFalse(info.IsValid);
        }

        [Test]
        public void FromAsteroid_SetsTargetTypeAsteroid()
        {
            var info = TargetInfo.FromAsteroid(10, "Asteroid #10", "Luminite");

            Assert.AreEqual(10, info.TargetId);
            Assert.AreEqual("Asteroid #10", info.DisplayName);
            Assert.AreEqual("Luminite", info.TypeLabel);
            Assert.AreEqual(TargetType.Asteroid, info.TargetType);
        }

        [Test]
        public void IsValid_ReturnsTrueForNonNegativeId()
        {
            var info = new TargetInfo(0, "Test", "Type", TargetType.Asteroid);
            Assert.IsTrue(info.IsValid);

            var info2 = new TargetInfo(100, "Test", "Type", TargetType.Station);
            Assert.IsTrue(info2.IsValid);
        }

        [Test]
        public void IsValid_ReturnsFalseForNegativeId()
        {
            var info = new TargetInfo(-1, "Test", "Type", TargetType.Asteroid);
            Assert.IsFalse(info.IsValid);

            var info2 = new TargetInfo(-99, "Test", "Type", TargetType.Station);
            Assert.IsFalse(info2.IsValid);
        }
    }
}
