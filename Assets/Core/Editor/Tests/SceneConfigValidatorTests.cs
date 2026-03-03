using System.Collections.Generic;
using NUnit.Framework;
using VoidHarvest.Core.Editor;

namespace VoidHarvest.Core.Editor.Tests
{
    [TestFixture]
    public class SceneConfigValidatorTests
    {
        [Test]
        public void ValidateFields_AllPresent_ReturnsNoErrors()
        {
            var fields = new Dictionary<string, bool>
            {
                { "worldDefinition", true },
                { "dockingConfig", true },
                { "targetingConfig", true }
            };

            var errors = SceneConfigValidatorLogic.ValidateFields(fields);
            Assert.AreEqual(0, errors.Count);
        }

        [Test]
        public void ValidateFields_NullField_ReportsError()
        {
            var fields = new Dictionary<string, bool>
            {
                { "worldDefinition", true },
                { "dockingConfig", false },
                { "targetingConfig", true }
            };

            var errors = SceneConfigValidatorLogic.ValidateFields(fields);
            Assert.AreEqual(1, errors.Count);
            Assert.IsTrue(errors[0].Contains("dockingConfig"));
        }

        [Test]
        public void ValidateFields_MultipleNullFields_ReportsAll()
        {
            var fields = new Dictionary<string, bool>
            {
                { "worldDefinition", false },
                { "dockingConfig", false },
                { "targetingConfig", true }
            };

            var errors = SceneConfigValidatorLogic.ValidateFields(fields);
            Assert.AreEqual(2, errors.Count);
        }

        [Test]
        public void ValidateFields_EmptyInput_ReturnsNoErrors()
        {
            var fields = new Dictionary<string, bool>();
            var errors = SceneConfigValidatorLogic.ValidateFields(fields);
            Assert.AreEqual(0, errors.Count);
        }

        [Test]
        public void ValidateFields_ReportsFieldName()
        {
            var fields = new Dictionary<string, bool>
            {
                { "miningVFXConfig", false }
            };

            var errors = SceneConfigValidatorLogic.ValidateFields(fields);
            Assert.AreEqual(1, errors.Count);
            Assert.IsTrue(errors[0].Contains("miningVFXConfig"));
        }
    }
}
