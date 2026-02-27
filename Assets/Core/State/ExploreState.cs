using VoidHarvest.Core.Extensions;

namespace VoidHarvest.Core.State
{
    /// <summary>
    /// Exploration state. Stub in MVP — ScannerActive is non-functional.
    /// </summary>
    public sealed record ExploreState(
        Option<int> CurrentFieldId,
        bool ScannerActive
    )
    {
        public static readonly ExploreState Empty = new(default, false);
    }
}
