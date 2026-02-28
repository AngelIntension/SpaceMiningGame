using UnityEngine;

namespace VoidHarvest.Features.Mining.Data
{
    /// <summary>
    /// Configuration for cosmetic ore chunk collection feedback.
    /// Spawn timing, chunk physics, and collection parameters.
    /// </summary>
    [CreateAssetMenu(menuName = "VoidHarvest/Mining/OreChunkConfig", fileName = "OreChunkConfig")]
    public class OreChunkConfig : ScriptableObject
    {
        [Header("Spawn Timing")]
        [Tooltip("Minimum seconds between spawns.")]
        public float SpawnIntervalMin = 3.0f;

        [Tooltip("Maximum seconds between spawns.")]
        public float SpawnIntervalMax = 7.0f;

        [Tooltip("Min chunks per spawn event.")]
        public int ChunksPerSpawnMin = 2;

        [Tooltip("Max chunks per spawn event.")]
        public int ChunksPerSpawnMax = 5;

        [Header("Chunk Appearance")]
        [Tooltip("Smallest chunk scale.")]
        public float ChunkScaleMin = 0.03f;

        [Tooltip("Largest chunk scale.")]
        public float ChunkScaleMax = 0.12f;

        [Header("Drift Phase")]
        [Tooltip("Seconds of outward drift.")]
        public float InitialDriftDuration = 0.75f;

        [Tooltip("Outward drift m/s.")]
        public float InitialDriftSpeed = 2.0f;

        [Header("Attraction Phase")]
        [Tooltip("Max attraction speed m/s.")]
        public float AttractionSpeed = 8.0f;

        [Tooltip("Attraction ramp-up m/s².")]
        public float AttractionAcceleration = 3.0f;

        [Header("Collection")]
        [Tooltip("Flash on barge arrival seconds.")]
        public float CollectionFlashDuration = 0.15f;

        [Tooltip("Force-despawn safety net seconds.")]
        public float MaxLifetime = 5.0f;

        [Tooltip("Emission intensity on chunks.")]
        public float GlowIntensity = 2.0f;
    }
}
