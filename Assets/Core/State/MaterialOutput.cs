namespace VoidHarvest.Core.State
{
    /// <summary>
    /// Final output from a completed refining job. MaterialId + quantity pair.
    /// See Spec 006: Station Services.
    /// </summary>
    public readonly struct MaterialOutput
    {
        public readonly string MaterialId;
        public readonly int Quantity;

        public MaterialOutput(string materialId, int quantity)
        {
            MaterialId = materialId;
            Quantity = quantity;
        }
    }
}
