using UnityEngine;

namespace VoidHarvest.Features.StationServices.Systems
{
    /// <summary>
    /// Pure static functions for repair cost calculation.
    /// See Spec 006: Station Services.
    /// </summary>
    public static class RepairMath
    {
        /// <summary>
        /// Calculate repair cost using ceiling rounding.
        /// At 100% integrity → 0, at 0% → repairCostPerHP, at 99.5% with 100 → 1 (ceiling).
        /// </summary>
        public static int CalculateRepairCost(float currentIntegrity, int repairCostPerHP)
        {
            return Mathf.CeilToInt((1.0f - currentIntegrity) * repairCostPerHP);
        }
    }
}
