using Unity.Mathematics;

namespace VoidHarvest.Core.EventBus.Events
{
    /// <summary>
    /// Published when a cosmetic ore chunk reaches the barge collector.
    /// Drives collection clink audio.
    /// </summary>
    public readonly struct OreChunkCollectedEvent
    {
        public readonly float3 Position;
        public readonly string OreId;

        public OreChunkCollectedEvent(float3 position, string oreId)
        {
            Position = position;
            OreId = oreId;
        }
    }
}
