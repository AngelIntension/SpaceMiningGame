using System.Collections.Immutable;
using NUnit.Framework;
using Unity.Mathematics;
using VoidHarvest.Core.Extensions;
using VoidHarvest.Features.Input.Data;

namespace VoidHarvest.Features.Input.Tests
{
    [TestFixture]
    public class PilotCommandTests
    {
        [Test]
        public void PilotCommand_IsImmutableRecord()
        {
            var cmd = new PilotCommand(
                Option<int>.Some(1),
                Option<float3>.Some(new float3(1, 2, 3)),
                default,
                new ThrustInput(0.5f, -0.3f, 0.1f),
                ImmutableArray.Create(1, 3)
            );

            // Record equality: same values = equal
            var cmd2 = new PilotCommand(
                Option<int>.Some(1),
                Option<float3>.Some(new float3(1, 2, 3)),
                default,
                new ThrustInput(0.5f, -0.3f, 0.1f),
                ImmutableArray.Create(1, 3)
            );

            Assert.AreEqual(cmd.SelectedTarget, cmd2.SelectedTarget);
            Assert.AreEqual(cmd.ManualThrust.Forward, cmd2.ManualThrust.Forward);
        }

        [Test]
        public void PilotCommand_Empty_HasNoTarget()
        {
            var cmd = PilotCommand.Empty;
            Assert.IsFalse(cmd.SelectedTarget.HasValue);
        }

        [Test]
        public void PilotCommand_Empty_HasNoAlignPoint()
        {
            var cmd = PilotCommand.Empty;
            Assert.IsFalse(cmd.AlignPoint.HasValue);
        }

        [Test]
        public void PilotCommand_Empty_HasNoRadialChoice()
        {
            var cmd = PilotCommand.Empty;
            Assert.IsFalse(cmd.RadialChoice.HasValue);
        }

        [Test]
        public void PilotCommand_Empty_HasZeroThrust()
        {
            var cmd = PilotCommand.Empty;
            Assert.AreEqual(0f, cmd.ManualThrust.Forward);
            Assert.AreEqual(0f, cmd.ManualThrust.Strafe);
            Assert.AreEqual(0f, cmd.ManualThrust.Roll);
        }

        [Test]
        public void PilotCommand_Empty_HasNoActivatedModules()
        {
            var cmd = PilotCommand.Empty;
            Assert.AreEqual(0, cmd.ActivatedModules.Length);
        }

        [Test]
        public void ThrustInput_StoresValues()
        {
            var thrust = new ThrustInput(0.8f, -0.5f, 0.3f);
            Assert.AreEqual(0.8f, thrust.Forward, 0.001f);
            Assert.AreEqual(-0.5f, thrust.Strafe, 0.001f);
            Assert.AreEqual(0.3f, thrust.Roll, 0.001f);
        }

        [Test]
        public void ThrustInput_Zero_AllZero()
        {
            var thrust = ThrustInput.Zero;
            Assert.AreEqual(0f, thrust.Forward);
            Assert.AreEqual(0f, thrust.Strafe);
            Assert.AreEqual(0f, thrust.Roll);
        }

        [Test]
        public void RadialMenuChoice_StoresValues()
        {
            var choice = new RadialMenuChoice(RadialMenuAction.Orbit, 100f);
            Assert.AreEqual(RadialMenuAction.Orbit, choice.Action);
            Assert.AreEqual(100f, choice.DistanceMeters, 0.001f);
        }

        [Test]
        public void PilotCommand_WithTarget_HasValue()
        {
            var cmd = PilotCommand.Empty with { SelectedTarget = Option<int>.Some(42) };
            Assert.IsTrue(cmd.SelectedTarget.HasValue);
            Assert.AreEqual(42, cmd.SelectedTarget.GetValueOrDefault(-1));
        }

        [Test]
        public void PilotCommand_WithModules_ContainsActivated()
        {
            var cmd = PilotCommand.Empty with { ActivatedModules = ImmutableArray.Create(1, 4, 7) };
            Assert.AreEqual(3, cmd.ActivatedModules.Length);
            Assert.AreEqual(1, cmd.ActivatedModules[0]);
            Assert.AreEqual(4, cmd.ActivatedModules[1]);
            Assert.AreEqual(7, cmd.ActivatedModules[2]);
        }
    }
}
