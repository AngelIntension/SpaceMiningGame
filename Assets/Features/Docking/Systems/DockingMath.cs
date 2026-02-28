using Unity.Mathematics;

namespace VoidHarvest.Features.Docking.Systems
{
    /// <summary>
    /// Pure static math functions for docking calculations.
    /// All functions are deterministic with no side effects.
    /// </summary>
    public static class DockingMath
    {
        /// <summary>
        /// Compute approach target position offset from the port along the ship→port direction.
        /// </summary>
        public static float3 ComputeApproachTarget(float3 shipPos, float3 portPos, float approachOffset)
        {
            var direction = math.normalizesafe(portPos - shipPos);
            return portPos - direction * approachOffset;
        }

        /// <summary>
        /// Compute smoothstep progress for snap animation. Returns [0, 1].
        /// </summary>
        public static float ComputeSnapProgress(float elapsed, float duration)
        {
            float t = math.saturate(elapsed / duration);
            return math.smoothstep(0f, 1f, t);
        }

        /// <summary>
        /// Interpolate position and rotation for snap animation using lerp/slerp.
        /// </summary>
        public static (float3 position, quaternion rotation) InterpolateSnapPose(
            float3 startPos, quaternion startRot,
            float3 targetPos, quaternion targetRot,
            float t)
        {
            var pos = math.lerp(startPos, targetPos, t);
            var rot = math.slerp(startRot, targetRot, t);
            return (pos, rot);
        }

        /// <summary>
        /// Compute the clearance position for undocking, offset along the port forward direction.
        /// </summary>
        public static float3 ComputeClearancePosition(float3 portPos, float3 portForward, float clearanceDistance)
        {
            return portPos + math.normalizesafe(portForward) * clearanceDistance;
        }

        /// <summary>
        /// Check if the ship is within docking initiation range.
        /// </summary>
        public static bool IsWithinDockingRange(float3 shipPos, float3 portPos, float maxRange)
        {
            return math.distancesq(shipPos, portPos) <= maxRange * maxRange;
        }

        /// <summary>
        /// Check if the ship is within snap range (magnetic snap zone).
        /// </summary>
        public static bool IsWithinSnapRange(float3 shipPos, float3 portPos, float snapRange)
        {
            return math.distancesq(shipPos, portPos) <= snapRange * snapRange;
        }
    }
}
