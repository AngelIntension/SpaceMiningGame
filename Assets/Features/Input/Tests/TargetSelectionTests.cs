using System.Collections.Immutable;
using NUnit.Framework;
using Unity.Mathematics;
using VoidHarvest.Core.Extensions;
using VoidHarvest.Features.Input.Data;

namespace VoidHarvest.Features.Input.Tests
{
    /// <summary>
    /// TDD tests for target selection data flow logic (T043).
    /// Covers PilotCommand construction for left-click target selection,
    /// double-click align, right-click radial menus, and edge cases.
    /// </summary>
    [TestFixture]
    public class TargetSelectionTests
    {
        // -----------------------------------------------------------
        // 1. Left-click populates SelectedTarget
        // -----------------------------------------------------------
        [Test]
        public void LeftClick_PopulatesSelectedTarget_HasValue()
        {
            const int targetId = 7;
            var cmd = new PilotCommand(
                Option<int>.Some(targetId),
                Option<float3>.None,
                Option<RadialMenuChoice>.None,
                ThrustInput.Zero,
                ImmutableArray<int>.Empty
            );

            Assert.IsTrue(cmd.SelectedTarget.HasValue);
            Assert.AreEqual(targetId, cmd.SelectedTarget.GetValueOrDefault(-1));
        }

        // -----------------------------------------------------------
        // 2. Double-click populates AlignPoint
        // -----------------------------------------------------------
        [Test]
        public void DoubleClick_PopulatesAlignPoint_HasCorrectPosition()
        {
            var worldPos = new float3(100f, -50f, 200f);
            var cmd = new PilotCommand(
                Option<int>.None,
                Option<float3>.Some(worldPos),
                Option<RadialMenuChoice>.None,
                ThrustInput.Zero,
                ImmutableArray<int>.Empty
            );

            Assert.IsTrue(cmd.AlignPoint.HasValue);
            var stored = cmd.AlignPoint.GetValueOrDefault(float3.zero);
            Assert.AreEqual(worldPos.x, stored.x, 0.001f);
            Assert.AreEqual(worldPos.y, stored.y, 0.001f);
            Assert.AreEqual(worldPos.z, stored.z, 0.001f);
        }

        // -----------------------------------------------------------
        // 3. Right-click sets RadialChoice (Approach)
        // -----------------------------------------------------------
        [Test]
        public void RightClick_SetsRadialChoice_Approach()
        {
            var choice = new RadialMenuChoice(RadialMenuAction.Approach, 50f);
            var cmd = new PilotCommand(
                Option<int>.None,
                Option<float3>.None,
                Option<RadialMenuChoice>.Some(choice),
                ThrustInput.Zero,
                ImmutableArray<int>.Empty
            );

            Assert.IsTrue(cmd.RadialChoice.HasValue);
            var stored = cmd.RadialChoice.GetValueOrDefault(default);
            Assert.AreEqual(RadialMenuAction.Approach, stored.Action);
            Assert.AreEqual(50f, stored.DistanceMeters, 0.001f);
        }

        // -----------------------------------------------------------
        // 4. RadialChoice Orbit with distance
        // -----------------------------------------------------------
        [Test]
        public void RadialChoice_Orbit_StoresCorrectDistance()
        {
            var choice = new RadialMenuChoice(RadialMenuAction.Orbit, 100f);
            var cmd = new PilotCommand(
                Option<int>.None,
                Option<float3>.None,
                Option<RadialMenuChoice>.Some(choice),
                ThrustInput.Zero,
                ImmutableArray<int>.Empty
            );

            Assert.IsTrue(cmd.RadialChoice.HasValue);
            var stored = cmd.RadialChoice.GetValueOrDefault(default);
            Assert.AreEqual(RadialMenuAction.Orbit, stored.Action);
            Assert.AreEqual(100f, stored.DistanceMeters, 0.001f);
        }

        // -----------------------------------------------------------
        // 5. RadialChoice KeepAtRange with distance
        // -----------------------------------------------------------
        [Test]
        public void RadialChoice_KeepAtRange_StoresCorrectDistance()
        {
            var choice = new RadialMenuChoice(RadialMenuAction.KeepAtRange, 50f);
            var cmd = new PilotCommand(
                Option<int>.None,
                Option<float3>.None,
                Option<RadialMenuChoice>.Some(choice),
                ThrustInput.Zero,
                ImmutableArray<int>.Empty
            );

            Assert.IsTrue(cmd.RadialChoice.HasValue);
            var stored = cmd.RadialChoice.GetValueOrDefault(default);
            Assert.AreEqual(RadialMenuAction.KeepAtRange, stored.Action);
            Assert.AreEqual(50f, stored.DistanceMeters, 0.001f);
        }

        // -----------------------------------------------------------
        // 6. Empty command has no target, align, or radial
        // -----------------------------------------------------------
        [Test]
        public void EmptyCommand_AllFieldsNoneOrZero()
        {
            var cmd = PilotCommand.Empty;

            Assert.IsFalse(cmd.SelectedTarget.HasValue);
            Assert.IsFalse(cmd.AlignPoint.HasValue);
            Assert.IsFalse(cmd.RadialChoice.HasValue);
            Assert.AreEqual(0f, cmd.ManualThrust.Forward);
            Assert.AreEqual(0f, cmd.ManualThrust.Strafe);
            Assert.AreEqual(0f, cmd.ManualThrust.Roll);
            Assert.AreEqual(0, cmd.ActivatedModules.Length);
        }

        // -----------------------------------------------------------
        // 7. Rapid target switching — last command wins
        // -----------------------------------------------------------
        [Test]
        public void RapidTargetSwitching_LastCommandHasCorrectTarget()
        {
            PilotCommand lastCmd = PilotCommand.Empty;

            for (int i = 0; i < 100; i++)
            {
                lastCmd = new PilotCommand(
                    Option<int>.Some(i),
                    Option<float3>.None,
                    Option<RadialMenuChoice>.None,
                    ThrustInput.Zero,
                    ImmutableArray<int>.Empty
                );
            }

            Assert.IsTrue(lastCmd.SelectedTarget.HasValue);
            Assert.AreEqual(99, lastCmd.SelectedTarget.GetValueOrDefault(-1));
        }

        // -----------------------------------------------------------
        // 8. PilotCommand immutability — with expression
        // -----------------------------------------------------------
        [Test]
        public void PilotCommand_WithExpression_ProducesNewInstance()
        {
            var original = new PilotCommand(
                Option<int>.Some(1),
                Option<float3>.Some(new float3(10f, 20f, 30f)),
                Option<RadialMenuChoice>.None,
                new ThrustInput(0.5f, 0f, 0f),
                ImmutableArray.Create(2, 5)
            );

            var updated = original with { SelectedTarget = Option<int>.Some(99) };

            // New instance has updated target
            Assert.AreEqual(99, updated.SelectedTarget.GetValueOrDefault(-1));

            // Original is unchanged
            Assert.AreEqual(1, original.SelectedTarget.GetValueOrDefault(-1));

            // Other fields carried over
            Assert.IsTrue(updated.AlignPoint.HasValue);
            var alignPos = updated.AlignPoint.GetValueOrDefault(float3.zero);
            Assert.AreEqual(10f, alignPos.x, 0.001f);
            Assert.AreEqual(20f, alignPos.y, 0.001f);
            Assert.AreEqual(30f, alignPos.z, 0.001f);
            Assert.AreEqual(0.5f, updated.ManualThrust.Forward, 0.001f);
            Assert.AreEqual(2, updated.ActivatedModules.Length);
        }

        // -----------------------------------------------------------
        // 9. Multiple fields populated simultaneously
        // -----------------------------------------------------------
        [Test]
        public void MultipleFieldsPopulated_TargetAndRadialChoice()
        {
            const int targetId = 42;
            var choice = new RadialMenuChoice(RadialMenuAction.Mine, 0f);

            var cmd = new PilotCommand(
                Option<int>.Some(targetId),
                Option<float3>.None,
                Option<RadialMenuChoice>.Some(choice),
                ThrustInput.Zero,
                ImmutableArray<int>.Empty
            );

            Assert.IsTrue(cmd.SelectedTarget.HasValue);
            Assert.AreEqual(targetId, cmd.SelectedTarget.GetValueOrDefault(-1));

            Assert.IsTrue(cmd.RadialChoice.HasValue);
            var stored = cmd.RadialChoice.GetValueOrDefault(default);
            Assert.AreEqual(RadialMenuAction.Mine, stored.Action);
        }

        // -----------------------------------------------------------
        // 10. RadialMenuChoice distance boundary values
        // -----------------------------------------------------------
        [Test]
        public void RadialMenuChoice_BoundaryDistance_10m()
        {
            var choice = new RadialMenuChoice(RadialMenuAction.KeepAtRange, 10f);
            Assert.AreEqual(RadialMenuAction.KeepAtRange, choice.Action);
            Assert.AreEqual(10f, choice.DistanceMeters, 0.001f);
        }

        [Test]
        public void RadialMenuChoice_BoundaryDistance_500m()
        {
            var choice = new RadialMenuChoice(RadialMenuAction.Orbit, 500f);
            Assert.AreEqual(RadialMenuAction.Orbit, choice.Action);
            Assert.AreEqual(500f, choice.DistanceMeters, 0.001f);
        }
    }
}
