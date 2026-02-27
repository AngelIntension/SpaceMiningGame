using NUnit.Framework;
using VoidHarvest.Core.Extensions;
using System;

namespace VoidHarvest.Core.Extensions.Tests
{
    [TestFixture]
    public class OptionTests
    {
        [Test]
        public void Some_HasValue_IsTrue()
        {
            var opt = Option<int>.Some(5);
            Assert.IsTrue(opt.HasValue);
        }

        [Test]
        public void None_HasValue_IsFalse()
        {
            var opt = Option<int>.None;
            Assert.IsFalse(opt.HasValue);
        }

        [Test]
        public void Match_WithSome_CallsSomeBranch()
        {
            var opt = Option<int>.Some(42);
            var result = opt.Match(v => v * 2, () => -1);
            Assert.AreEqual(84, result);
        }

        [Test]
        public void Match_WithNone_CallsNoneBranch()
        {
            var opt = Option<int>.None;
            var result = opt.Match(v => v * 2, () => -1);
            Assert.AreEqual(-1, result);
        }

        [Test]
        public void Map_WithSome_TransformsValue()
        {
            var opt = Option<int>.Some(5);
            var mapped = opt.Map(v => v.ToString());
            Assert.AreEqual("5", mapped.GetValueOrDefault(""));
        }

        [Test]
        public void Map_WithNone_ReturnsNone()
        {
            var opt = Option<int>.None;
            var mapped = opt.Map(v => v.ToString());
            Assert.IsFalse(mapped.HasValue);
        }

        [Test]
        public void FlatMap_WithSome_ChainsValues()
        {
            var opt = Option<int>.Some(5);
            var result = opt.FlatMap(v => v > 0 ? Option<string>.Some("positive") : Option<string>.None);
            Assert.AreEqual("positive", result.GetValueOrDefault(""));
        }

        [Test]
        public void FlatMap_WithNone_ReturnsNone()
        {
            var opt = Option<int>.None;
            var result = opt.FlatMap(v => Option<string>.Some("x"));
            Assert.IsFalse(result.HasValue);
        }

        [Test]
        public void GetValueOrDefault_WithSome_ReturnsValue()
        {
            var opt = Option<int>.Some(7);
            Assert.AreEqual(7, opt.GetValueOrDefault(0));
        }

        [Test]
        public void GetValueOrDefault_WithNone_ReturnsDefault()
        {
            var opt = Option<int>.None;
            Assert.AreEqual(99, opt.GetValueOrDefault(99));
        }

        [Test]
        public void Equality_SameValues_AreEqual()
        {
            Assert.AreEqual(Option<int>.Some(5), Option<int>.Some(5));
        }

        [Test]
        public void Equality_DifferentValues_AreNotEqual()
        {
            Assert.AreNotEqual(Option<int>.Some(5), Option<int>.Some(3));
        }

        [Test]
        public void Equality_BothNone_AreEqual()
        {
            Assert.AreEqual(Option<int>.None, Option<int>.None);
        }

        [Test]
        public void Equality_SomeAndNone_AreNotEqual()
        {
            Assert.AreNotEqual(Option<int>.Some(5), Option<int>.None);
        }

        [Test]
        public void ImplicitConversion_FromValue_CreatesSome()
        {
            Option<string> opt = "hello";
            Assert.IsTrue(opt.HasValue);
            Assert.AreEqual("hello", opt.GetValueOrDefault(""));
        }

        [Test]
        public void ImplicitConversion_FromNull_CreatesNone()
        {
            Option<string> opt = (string)null;
            Assert.IsFalse(opt.HasValue);
        }

        [Test]
        public void Some_WithNull_ThrowsArgumentNullException()
        {
            Assert.Throws<ArgumentNullException>(() => Option<string>.Some(null));
        }

        [Test]
        public void MatchAction_WithSome_CallsSomeBranch()
        {
            var opt = Option<int>.Some(10);
            int result = 0;
            opt.Match(v => result = v, () => result = -1);
            Assert.AreEqual(10, result);
        }

        [Test]
        public void MatchAction_WithNone_CallsNoneBranch()
        {
            var opt = Option<int>.None;
            int result = 0;
            opt.Match(v => result = v, () => result = -1);
            Assert.AreEqual(-1, result);
        }

        [Test]
        public void ToString_Some_ShowsValue()
        {
            Assert.AreEqual("Some(42)", Option<int>.Some(42).ToString());
        }

        [Test]
        public void ToString_None_ShowsNone()
        {
            Assert.AreEqual("None", Option<int>.None.ToString());
        }

        [Test]
        public void Default_IsNone()
        {
            Option<int> opt = default;
            Assert.IsFalse(opt.HasValue);
        }
    }
}
