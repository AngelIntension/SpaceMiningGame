using System.Collections.Immutable;
using VoidHarvest.Core.Extensions;

namespace VoidHarvest.Core.State
{
    /// <summary>
    /// Fleet state. Stub — Phase 1+.
    /// </summary>
    public sealed record FleetState(
        ImmutableArray<OwnedShip> Ships,
        string ActiveShipId,
        Option<int> DockedAtStation
    )
    {
        public static readonly FleetState Empty = new(
            ImmutableArray<OwnedShip>.Empty, "", default
        );
    }

    /// <summary>
    /// Immutable record for a ship owned by the player. Phase 1+.
    /// </summary>
    public sealed record OwnedShip(
        string ShipId,
        string ArchetypeId,
        ImmutableArray<ModuleSlot> EquippedModules,
        float MaxThrust,
        float MaxSpeed,
        float MiningPower,
        float HullIntegrity,
        InventoryState Cargo
    );

    /// <summary>
    /// A module equipment slot on a ship. Phase 1+.
    /// </summary>
    public readonly struct ModuleSlot
    {
        /// <summary>Zero-based slot position on the ship hull. Phase 1+.</summary>
        public readonly int SlotIndex;
        /// <summary>Module ID installed in this slot, or None if empty. Phase 1+.</summary>
        public readonly Option<string> ModuleId;
        /// <summary>Type restriction for this slot. Phase 1+.</summary>
        public readonly ModuleType Type;

        /// <summary>
        /// Create a module slot with the given index, optional module, and type. Phase 1+.
        /// </summary>
        public ModuleSlot(int slotIndex, Option<string> moduleId, ModuleType type)
        {
            SlotIndex = slotIndex;
            ModuleId = moduleId;
            Type = type;
        }
    }

    /// <summary>
    /// Classification of ship module types. Phase 1+.
    /// </summary>
    public enum ModuleType
    {
        MiningLaser, Shield, Scanner, Thruster, Weapon, Utility
    }
}
