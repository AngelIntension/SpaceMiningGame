namespace VoidHarvest.Core.State
{
    /// <summary>
    /// Captured output parameters for a refining job. Immutable snapshot of ore's output config at job creation.
    /// See Spec 006: Station Services.
    /// </summary>
    public readonly struct RefiningOutputConfig
    {
        public readonly string MaterialId;
        public readonly int BaseYieldPerUnit;
        public readonly int VarianceMin;
        public readonly int VarianceMax;

        public RefiningOutputConfig(string materialId, int baseYieldPerUnit, int varianceMin, int varianceMax)
        {
            MaterialId = materialId;
            BaseYieldPerUnit = baseYieldPerUnit;
            VarianceMin = varianceMin;
            VarianceMax = varianceMax;
        }
    }
}
