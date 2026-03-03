using UnityEngine;

namespace VoidHarvest.Features.Targeting.Views
{
    /// <summary>
    /// Visual effect parameters for targeting system.
    /// See Spec 007: In-Flight Targeting.
    /// </summary>
    [CreateAssetMenu(menuName = "VoidHarvest/Targeting VFX Config")]
    public class TargetingVFXConfig : ScriptableObject
    {
        /// <summary>Duration of the lock confirmation flash in seconds.</summary>
        public float LockFlashDuration = 0.3f;
        /// <summary>Speed of reticle corner pulsing during lock acquisition.</summary>
        public float ReticlePulseSpeed = 2.0f;
    }
}
