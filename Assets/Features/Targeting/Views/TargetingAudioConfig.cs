using UnityEngine;

namespace VoidHarvest.Features.Targeting.Views
{
    /// <summary>
    /// Audio clip references for targeting feedback.
    /// See Spec 007: In-Flight Targeting.
    /// </summary>
    [CreateAssetMenu(menuName = "VoidHarvest/Targeting Audio Config")]
    public class TargetingAudioConfig : ScriptableObject
    {
        /// <summary>Rising tone played during lock acquisition.</summary>
        public AudioClip LockAcquiringClip;
        /// <summary>Confirmation sound on successful lock.</summary>
        public AudioClip LockConfirmedClip;
        /// <summary>Failure sound on lock cancellation.</summary>
        public AudioClip LockFailedClip;
        /// <summary>Sound when all lock slots are full.</summary>
        public AudioClip LockSlotsFullClip;
        /// <summary>Sound when a locked target is destroyed.</summary>
        public AudioClip TargetLostClip;
    }
}
