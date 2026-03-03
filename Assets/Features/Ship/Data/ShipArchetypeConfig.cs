using UnityEngine;

namespace VoidHarvest.Features.Ship.Data
{
    /// <summary>
    /// Static data definition for a ship archetype. Authored in Unity Editor.
    /// See MVP-01: 6DOF Newtonian flight.
    /// </summary>
    [CreateAssetMenu(menuName = "VoidHarvest/Ship/Ship Archetype Config")]
    public class ShipArchetypeConfig : ScriptableObject
    {
        /// <summary>Unique identifier for this ship archetype. See MVP-01.</summary>
        public string ArchetypeId;
        /// <summary>Human-readable name shown in HUD. See MVP-09.</summary>
        public string DisplayName;
        /// <summary>Ship class specialization role. See MVP-01.</summary>
        public ShipRole Role;
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
        /// <summary>Mining yield multiplier from this archetype. See MVP-05.</summary>
        public float MiningPower;
        /// <summary>Number of module equipment slots on this ship. Phase 1+.</summary>
        public int ModuleSlots;
        /// <summary>Maximum cargo volume in cubic meters. See MVP-06.</summary>
        public float CargoCapacity;
        /// <summary>Number of cargo inventory slots. See Spec 009 US4.</summary>
        public int CargoSlots = 20;
        /// <summary>Visual hull mesh reference. See MVP-01.</summary>
        public Mesh HullMesh;
        /// <summary>Visual hull material reference. See MVP-01.</summary>
        public Material HullMaterial;

        /// <summary>Seconds to acquire a target lock. See Spec 007.</summary>
        public float BaseLockTime = 1.5f;
        /// <summary>Maximum simultaneous target locks. See Spec 007.</summary>
        public int MaxTargetLocks = 3;
        /// <summary>Maximum range in meters for lock acquisition. See Spec 007.</summary>
        public float MaxLockRange = 5000f;

        private void OnValidate()
        {
            if (string.IsNullOrEmpty(ArchetypeId))
                Debug.LogWarning($"[{name}] ArchetypeId must not be empty");
            if (string.IsNullOrEmpty(DisplayName))
                Debug.LogWarning($"[{name}] DisplayName must not be empty");
            if (Mass <= 0f)
                Debug.LogWarning($"[{name}] Mass must be > 0");
            if (MaxThrust <= 0f)
                Debug.LogWarning($"[{name}] MaxThrust must be > 0");
            if (MaxSpeed <= 0f)
                Debug.LogWarning($"[{name}] MaxSpeed must be > 0");
            if (RotationTorque <= 0f)
                Debug.LogWarning($"[{name}] RotationTorque must be > 0");
            if (LinearDamping < 0f)
                Debug.LogWarning($"[{name}] LinearDamping must be >= 0");
            if (AngularDamping < 0f)
                Debug.LogWarning($"[{name}] AngularDamping must be >= 0");
            if (CargoCapacity <= 0f)
                Debug.LogWarning($"[{name}] CargoCapacity must be > 0");
            if (CargoSlots < 1)
                Debug.LogWarning($"[{name}] CargoSlots must be >= 1");
            else if (CargoSlots > 100)
                Debug.Log($"[{name}] CargoSlots is {CargoSlots} — above soft threshold of 100");
            if (BaseLockTime <= 0f)
                Debug.LogWarning($"[{name}] BaseLockTime must be > 0");
            if (MaxTargetLocks < 1)
                Debug.LogWarning($"[{name}] MaxTargetLocks must be >= 1");
            if (MaxLockRange <= 0f)
                Debug.LogWarning($"[{name}] MaxLockRange must be > 0");
        }
    }

    /// <summary>
    /// Ship class specialization.
    /// </summary>
    public enum ShipRole
    {
        MiningBarge,
        Hauler,
        CombatScout,
        Explorer,
        Refinery
    }
}
