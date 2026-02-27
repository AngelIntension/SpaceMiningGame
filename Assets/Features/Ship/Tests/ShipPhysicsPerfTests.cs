using System.Diagnostics;
using NUnit.Framework;
using Unity.Mathematics;
using VoidHarvest.Core.State;
using VoidHarvest.Features.Ship.Systems;

namespace VoidHarvest.Features.Ship.Tests
{
    /// <summary>
    /// Performance test: simulates 500 ships through the physics math pipeline.
    /// Asserts total frame time under 2ms.
    /// See T075: 500-ship performance test.
    /// </summary>
    [TestFixture]
    public class ShipPhysicsPerfTests
    {
        private const int ShipCount = 500;
        private const float DeltaTime = 0.016f; // 60 FPS
        private const float MaxThrust = 5000f;
        private const float RotationTorque = 2000f;
        private const float Mass = 1000f;
        private const float MaxSpeed = 300f;
        private const float LinearDamping = 0.5f;
        private const float AngularDamping = 2f;

        private float3[] _positions;
        private quaternion[] _rotations;
        private float3[] _velocities;
        private float3[] _angularVelocities;

        [SetUp]
        public void SetUp()
        {
            var rng = new Random(42);

            _positions = new float3[ShipCount];
            _rotations = new quaternion[ShipCount];
            _velocities = new float3[ShipCount];
            _angularVelocities = new float3[ShipCount];

            for (int i = 0; i < ShipCount; i++)
            {
                _positions[i] = rng.NextFloat3(-1000f, 1000f);
                _rotations[i] = rng.NextQuaternionRotation();
                _velocities[i] = rng.NextFloat3(-50f, 50f);
                _angularVelocities[i] = rng.NextFloat3(-1f, 1f);
            }
        }

        [Test]
        public void SimulateFrame_500Ships_Under2ms()
        {
            // Warm up (JIT, Burst, caches)
            RunSimulationFrame(0.5f, 0f, 0f);
            RunSimulationFrame(0.5f, 0f, 0f);

            var sw = Stopwatch.StartNew();
            const int frames = 100;

            for (int f = 0; f < frames; f++)
            {
                // Vary inputs per frame to prevent trivial optimization
                float forward = math.sin(f * 0.1f);
                float strafe = math.cos(f * 0.1f) * 0.5f;
                float roll = math.sin(f * 0.2f) * 0.3f;

                RunSimulationFrame(forward, strafe, roll);
            }

            sw.Stop();
            double avgFrameMs = sw.Elapsed.TotalMilliseconds / frames;

            Assert.Less(avgFrameMs, 2.0,
                $"Average frame time for {ShipCount} ships: {avgFrameMs:F3}ms exceeds 2ms target");
        }

        [Test]
        public void SimulateFrame_500Ships_AutoPilot_Under2ms()
        {
            // Test auto-pilot modes (align, approach, orbit) which have more math
            RunSimulationFrameAutoPilot(ShipFlightMode.Approach, 0);
            RunSimulationFrameAutoPilot(ShipFlightMode.Approach, 0);

            var sw = Stopwatch.StartNew();
            const int frames = 100;

            for (int f = 0; f < frames; f++)
            {
                var mode = (f % 3) switch
                {
                    0 => ShipFlightMode.Approach,
                    1 => ShipFlightMode.Orbit,
                    _ => ShipFlightMode.KeepAtRange
                };
                RunSimulationFrameAutoPilot(mode, f);
            }

            sw.Stop();
            double avgFrameMs = sw.Elapsed.TotalMilliseconds / frames;

            Assert.Less(avgFrameMs, 2.0,
                $"Average autopilot frame time for {ShipCount} ships: {avgFrameMs:F3}ms exceeds 2ms target");
        }

        private void RunSimulationFrame(float forward, float strafe, float roll)
        {
            for (int i = 0; i < ShipCount; i++)
            {
                var pos = _positions[i];
                var rot = _rotations[i];
                var vel = _velocities[i];
                var angVel = _angularVelocities[i];

                var fwd = math.forward(rot);
                var right = math.mul(rot, math.right());
                var up = math.mul(rot, math.up());

                var mode = ShipPhysicsMath.DetermineFlightMode(
                    ShipFlightMode.Idle, forward, strafe, roll, false, -1);

                var thrust = ShipPhysicsMath.ComputeThrust(
                    fwd, right, up, forward, strafe, roll, MaxThrust, mode);

                var torque = ShipPhysicsMath.ComputeTorque(
                    fwd, up, forward, strafe, roll, RotationTorque, mode);

                vel = ShipPhysicsMath.ApplyForce(vel, thrust, Mass, DeltaTime);
                vel = ShipPhysicsMath.ApplyDamping(vel, LinearDamping, DeltaTime);
                vel = ShipPhysicsMath.ClampSpeed(vel, MaxSpeed);
                vel = ShipPhysicsMath.SanitizeVelocity(vel);

                angVel = ShipPhysicsMath.ApplyForce(angVel, torque, Mass, DeltaTime);
                angVel = ShipPhysicsMath.ApplyDamping(angVel, AngularDamping, DeltaTime);
                angVel = ShipPhysicsMath.SanitizeVelocity(angVel);

                _positions[i] = pos + vel * DeltaTime;
                _rotations[i] = ShipPhysicsMath.IntegrateRotation(rot, angVel, DeltaTime);
                _velocities[i] = vel;
                _angularVelocities[i] = angVel;
            }
        }

        private void RunSimulationFrameAutoPilot(ShipFlightMode mode, int seed)
        {
            var targetPos = new float3(seed * 10f, 0f, seed * 5f);

            for (int i = 0; i < ShipCount; i++)
            {
                var pos = _positions[i];
                var rot = _rotations[i];
                var vel = _velocities[i];
                var angVel = _angularVelocities[i];

                var fwd = math.forward(rot);
                var toTarget = math.normalizesafe(targetPos - pos);
                float distance = math.length(targetPos - pos);

                var torque = ShipPhysicsMath.ComputeAlignTorque(fwd, toTarget, RotationTorque);

                float3 thrust = mode switch
                {
                    ShipFlightMode.Approach => ShipPhysicsMath.ComputeApproachThrust(
                        fwd, toTarget, distance, 50f, MaxThrust),
                    ShipFlightMode.Orbit => ShipPhysicsMath.ComputeOrbitThrust(
                        fwd, toTarget, distance, 100f, MaxThrust),
                    ShipFlightMode.KeepAtRange => ShipPhysicsMath.ComputeKeepAtRangeThrust(
                        toTarget, distance, 50f, MaxThrust),
                    _ => float3.zero
                };

                vel = ShipPhysicsMath.ApplyForce(vel, thrust, Mass, DeltaTime);
                vel = ShipPhysicsMath.ApplyDamping(vel, LinearDamping, DeltaTime);
                vel = ShipPhysicsMath.ClampSpeed(vel, MaxSpeed);
                vel = ShipPhysicsMath.SanitizeVelocity(vel);

                angVel = ShipPhysicsMath.ApplyForce(angVel, torque, Mass, DeltaTime);
                angVel = ShipPhysicsMath.ApplyDamping(angVel, AngularDamping, DeltaTime);
                angVel = ShipPhysicsMath.SanitizeVelocity(angVel);

                _positions[i] = pos + vel * DeltaTime;
                _rotations[i] = ShipPhysicsMath.IntegrateRotation(rot, angVel, DeltaTime);
                _velocities[i] = vel;
                _angularVelocities[i] = angVel;
            }
        }
    }
}
