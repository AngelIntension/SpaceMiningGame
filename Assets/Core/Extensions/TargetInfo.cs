namespace VoidHarvest.Core.Extensions
{
    /// <summary>
    /// Immutable snapshot of display data for any targetable object.
    /// Common output from both ITargetable MonoBehaviours and ECS asteroid queries.
    /// See Spec 007: In-Flight Targeting.
    /// </summary>
    public readonly struct TargetInfo
    {
        public readonly int TargetId;
        public readonly string DisplayName;
        public readonly string TypeLabel;
        public readonly TargetType TargetType;

        /// <summary>True if this represents a valid target (TargetId >= 0).</summary>
        public bool IsValid => TargetId >= 0;

        /// <summary>Sentinel for "no target".</summary>
        public static readonly TargetInfo None = new TargetInfo(-1, string.Empty, string.Empty, TargetType.None);

        public TargetInfo(int targetId, string displayName, string typeLabel, TargetType targetType)
        {
            TargetId = targetId;
            DisplayName = displayName ?? string.Empty;
            TypeLabel = typeLabel ?? string.Empty;
            TargetType = targetType;
        }

        /// <summary>Construct from an ITargetable MonoBehaviour.</summary>
        public static TargetInfo From(ITargetable target)
        {
            if (target == null) return None;
            return new TargetInfo(target.TargetId, target.DisplayName, target.TypeLabel, target.TargetType);
        }

        /// <summary>Construct from ECS asteroid data.</summary>
        public static TargetInfo FromAsteroid(int entityIndex, string displayName, string oreTypeName)
        {
            return new TargetInfo(entityIndex, displayName ?? string.Empty, oreTypeName ?? string.Empty, TargetType.Asteroid);
        }
    }
}
