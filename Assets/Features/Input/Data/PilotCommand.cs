using System.Collections.Immutable;
using VoidHarvest.Core.Extensions;
using Unity.Mathematics;

namespace VoidHarvest.Features.Input.Data
{
    /// <summary>
    /// Immutable command record constructed from player input each frame.
    /// See MVP-01: 6DOF Newtonian flight, MVP-03: Target selection.
    /// </summary>
    public sealed record PilotCommand(
        Option<int> SelectedTarget,
        Option<float3> AlignPoint,
        Option<RadialMenuChoice> RadialChoice,
        ThrustInput ManualThrust,
        ImmutableArray<int> ActivatedModules
    )
    {
        public static readonly PilotCommand Empty = new(
            default,
            default,
            default,
            default,
            ImmutableArray<int>.Empty
        );
    }

    /// <summary>
    /// 6DOF thrust input. Forward=W/S [-1,1], Strafe=A/D [-1,1], Roll=Q/E [-1,1].
    /// Readonly struct because C# 9.0 lacks record struct.
    /// </summary>
    public readonly struct ThrustInput
    {
        /// <summary>Forward/backward thrust [-1, 1]. See MVP-01.</summary>
        public readonly float Forward;
        /// <summary>Left/right strafe [-1, 1]. See MVP-01.</summary>
        public readonly float Strafe;
        /// <summary>Roll [-1, 1]. See MVP-01.</summary>
        public readonly float Roll;

        /// <summary>
        /// Create a thrust input tuple. See MVP-01: 6DOF Newtonian flight.
        /// </summary>
        public ThrustInput(float forward, float strafe, float roll)
        {
            Forward = forward;
            Strafe = strafe;
            Roll = roll;
        }

        /// <summary>Zero thrust on all axes.</summary>
        public static readonly ThrustInput Zero = new(0f, 0f, 0f);
    }

    /// <summary>
    /// Radial context menu actions. See MVP-04.
    /// </summary>
    public enum RadialMenuAction
    {
        Approach,
        Orbit,
        Mine,
        KeepAtRange,
        Dock,
        Warp
    }

    /// <summary>
    /// Player's choice from radial context menu with distance parameter.
    /// </summary>
    public readonly struct RadialMenuChoice
    {
        /// <summary>The chosen radial menu action. See MVP-04.</summary>
        public readonly RadialMenuAction Action;
        /// <summary>Distance parameter in meters for the action. See MVP-04.</summary>
        public readonly float DistanceMeters;

        /// <summary>
        /// Create a radial menu choice with action and distance. See MVP-04: Auto-pilot modes.
        /// </summary>
        public RadialMenuChoice(RadialMenuAction action, float distanceMeters)
        {
            Action = action;
            DistanceMeters = distanceMeters;
        }
    }
}
