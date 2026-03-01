namespace VoidHarvest.Core.Extensions
{
    /// <summary>
    /// Discriminates the type of the player's current target. See spec 004.
    /// Lives in Core/Extensions so both EventBus and State assemblies can reference it.
    /// </summary>
    public enum TargetType
    {
        None = 0,
        Asteroid = 1,
        Station = 2
    }
}
