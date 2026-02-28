using Unity.Mathematics;

namespace VoidHarvest.Core.EventBus.Events
{
    /// <summary>
    /// Published when an asteroid crosses a 25% depletion threshold.
    /// Drives crumble burst VFX, HUD flash, and audio rumble.
    /// </summary>
    public readonly struct ThresholdCrossedEvent
    {
        public readonly int AsteroidId;
        public readonly byte ThresholdIndex;
        public readonly float3 Position;
        public readonly float AsteroidRadius;

        public ThresholdCrossedEvent(int asteroidId, byte thresholdIndex, float3 position, float asteroidRadius)
        {
            AsteroidId = asteroidId;
            ThresholdIndex = thresholdIndex;
            Position = position;
            AsteroidRadius = asteroidRadius;
        }
    }
}
