namespace VoidHarvest.Core.Extensions
{
    /// <summary>
    /// Cross-cutting contract for MonoBehaviour-based targetable objects.
    /// ECS entities use TargetInfo adapter functions instead.
    /// See Spec 007: In-Flight Targeting.
    /// </summary>
    public interface ITargetable
    {
        /// <summary>Unique identifier (typically GameObject.GetInstanceID()).</summary>
        int TargetId { get; }
        /// <summary>Human-readable name for reticle/card display.</summary>
        string DisplayName { get; }
        /// <summary>Category label (e.g., "Station", "Luminite").</summary>
        string TypeLabel { get; }
        /// <summary>Enum discriminator (Asteroid, Station, etc.).</summary>
        TargetType TargetType { get; }
    }
}
