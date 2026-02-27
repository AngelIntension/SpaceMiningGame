using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;
using VoidHarvest.Features.Ship.Data;
using VoidHarvest.Core.State;

namespace VoidHarvest.Features.Ship.Views
{
    /// <summary>
    /// Authoring component for the player ship entity. Place on a GameObject in a SubScene.
    /// Baker adds all required ECS components during baking.
    /// See MVP-01: 6DOF Newtonian flight.
    /// </summary>
    public class ShipAuthoring : MonoBehaviour
    {
        [Header("Ship Config")]
        /// <summary>Ship mass in kg for Newtonian physics. See MVP-01.</summary>
        public float Mass = 1000f;
        /// <summary>Maximum linear thrust in Newtons. See MVP-01.</summary>
        public float MaxThrust = 5000f;
        /// <summary>Speed cap in m/s. See MVP-01.</summary>
        public float MaxSpeed = 100f;
        /// <summary>Maximum rotational torque. See MVP-01.</summary>
        public float RotationTorque = 2000f;
        /// <summary>Linear velocity damping per second. See MVP-01.</summary>
        public float LinearDamping = 0.5f;
        /// <summary>Angular velocity damping per second. See MVP-01.</summary>
        public float AngularDamping = 2.0f;
        /// <summary>Mining yield multiplier. See MVP-05.</summary>
        public float MiningPower = 1.0f;

        [Header("Initial State")]
        /// <summary>World-space spawn position. See MVP-01.</summary>
        public Vector3 StartPosition = Vector3.zero;
    }

    /// <summary>
    /// Baker for ShipAuthoring. Adds all ship ECS components.
    /// The ship entity is simulation-only (no RenderMesh — visual is a separate GameObject with ShipView).
    /// </summary>
    public class ShipBaker : Baker<ShipAuthoring>
    {
        public override void Bake(ShipAuthoring authoring)
        {
            var entity = GetEntity(TransformUsageFlags.Dynamic);

            AddComponent(entity, new ShipPositionComponent
            {
                Position = new float3(authoring.StartPosition.x, authoring.StartPosition.y, authoring.StartPosition.z),
                Rotation = quaternion.identity
            });

            AddComponent(entity, new ShipVelocityComponent
            {
                Velocity = float3.zero,
                AngularVelocity = float3.zero
            });

            AddComponent(entity, new ShipConfigComponent
            {
                Mass = authoring.Mass,
                MaxThrust = authoring.MaxThrust,
                MaxSpeed = authoring.MaxSpeed,
                RotationTorque = authoring.RotationTorque,
                LinearDamping = authoring.LinearDamping,
                AngularDamping = authoring.AngularDamping,
                MiningPower = authoring.MiningPower
            });

            AddComponent(entity, new ShipFlightModeComponent
            {
                Mode = ShipFlightMode.Idle
            });

            AddComponent(entity, new PilotCommandComponent
            {
                Forward = 0f,
                Strafe = 0f,
                Roll = 0f,
                SelectedTargetId = -1,
                AlignPoint = float3.zero,
                HasAlignPoint = false,
                RadialAction = -1,
                RadialDistance = 0f
            });

            AddComponent(entity, new PlayerControlledTag());
        }
    }
}
