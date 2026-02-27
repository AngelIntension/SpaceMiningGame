using Unity.Entities;
using Unity.Mathematics;
using VoidHarvest.Core.State;

namespace VoidHarvest.Features.Ship.Data
{
    /// <summary>
    /// ECS position/rotation component. Canonical simulation data.
    /// // CONSTITUTION DEVIATION: ECS mutable shell
    /// </summary>
    public struct ShipPositionComponent : IComponentData
    {
        /// <summary>World-space position of the ship. See MVP-01: 6DOF Newtonian flight.</summary>
        public float3 Position;
        /// <summary>World-space rotation of the ship. See MVP-01: 6DOF Newtonian flight.</summary>
        public quaternion Rotation;
    }

    /// <summary>
    /// ECS velocity component. Updated by ShipPhysicsSystem.
    /// // CONSTITUTION DEVIATION: ECS mutable shell
    /// </summary>
    public struct ShipVelocityComponent : IComponentData
    {
        /// <summary>Linear velocity in world-space m/s. See MVP-01: 6DOF Newtonian flight.</summary>
        public float3 Velocity;
        /// <summary>Angular velocity in rad/s. See MVP-01: 6DOF Newtonian flight.</summary>
        public float3 AngularVelocity;
    }

    /// <summary>
    /// ECS ship configuration component. Set during baking from ShipArchetypeConfig.
    /// // CONSTITUTION DEVIATION: ECS mutable shell
    /// </summary>
    public struct ShipConfigComponent : IComponentData
    {
        /// <summary>Ship mass in kg for Newtonian F=ma. See MVP-01.</summary>
        public float Mass;
        /// <summary>Maximum linear thrust force in Newtons. See MVP-01.</summary>
        public float MaxThrust;
        /// <summary>Speed cap in m/s. See MVP-01.</summary>
        public float MaxSpeed;
        /// <summary>Maximum rotational torque. See MVP-01.</summary>
        public float RotationTorque;
        /// <summary>Linear velocity damping factor per second. See MVP-01.</summary>
        public float LinearDamping;
        /// <summary>Angular velocity damping factor per second. See MVP-01.</summary>
        public float AngularDamping;
        /// <summary>Mining yield multiplier from ship archetype. See MVP-05.</summary>
        public float MiningPower;
    }

    /// <summary>
    /// ECS flight mode component. Determined by ShipPhysicsMath.DetermineFlightMode.
    /// // CONSTITUTION DEVIATION: ECS mutable shell
    /// </summary>
    public struct ShipFlightModeComponent : IComponentData
    {
        /// <summary>Current flight automation mode. See MVP-04: Auto-pilot modes.</summary>
        public ShipFlightMode Mode;
    }

    /// <summary>
    /// ECS pilot command input component. Written by InputBridge each frame.
    /// // CONSTITUTION DEVIATION: ECS mutable shell
    /// </summary>
    public struct PilotCommandComponent : IComponentData
    {
        /// <summary>Forward/backward thrust input [-1, 1]. See MVP-01.</summary>
        public float Forward;
        /// <summary>Left/right strafe input [-1, 1]. See MVP-01.</summary>
        public float Strafe;
        /// <summary>Roll input [-1, 1]. See MVP-01.</summary>
        public float Roll;
        /// <summary>Instance ID of the currently selected target, or -1. See MVP-03.</summary>
        public int SelectedTargetId;
        /// <summary>World-space point to align toward (double-click target). See MVP-04.</summary>
        public float3 AlignPoint;
        /// <summary>True if AlignPoint is valid this frame. See MVP-04.</summary>
        public bool HasAlignPoint;
        /// <summary>Radial menu action index, or -1 if none. See MVP-04.</summary>
        public int RadialAction;
        /// <summary>Distance parameter for radial menu actions. See MVP-04.</summary>
        public float RadialDistance;
    }

    /// <summary>
    /// Tag component to identify the player-controlled ship entity.
    /// </summary>
    public struct PlayerControlledTag : IComponentData { }
}
