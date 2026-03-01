using Unity.Entities;

namespace VoidHarvest.Features.Mining.Data
{
    /// <summary>
    /// Baked ore type data. Burst-accessible via BlobAssetReference.
    /// Baked from OreDefinition ScriptableObject at initialization.
    /// See MVP-05: Mining beam and yield.
    /// </summary>
    public struct OreTypeBlob
    {
        /// <summary>Base ore yield per second before modifiers. See Spec 005.</summary>
        public float BaseYieldPerSecond;
        /// <summary>Extraction difficulty multiplier (denominator in yield formula). See Spec 005.</summary>
        public float Hardness;
        /// <summary>Cargo volume consumed per unit mined. See Spec 005.</summary>
        public float VolumePerUnit;
    }

    /// <summary>
    /// Database of all ore types as a BlobAsset array.
    /// </summary>
    public struct OreTypeBlobDatabase
    {
        /// <summary>Array of all ore type data entries. See MVP-05.</summary>
        public BlobArray<OreTypeBlob> OreTypes;
    }

    /// <summary>
    /// Singleton component holding the BlobAssetReference to OreTypeBlobDatabase.
    /// Created by OreTypeBlobBakingSystem (T057b).
    /// </summary>
    public struct OreTypeDatabaseComponent : IComponentData
    {
        /// <summary>Reference to the baked ore type BlobAsset. See MVP-05.</summary>
        public BlobAssetReference<OreTypeBlobDatabase> Database;
    }
}
