using UnityEngine;

namespace VoidHarvest.Features.Targeting.Data
{
    /// <summary>
    /// Global targeting visual configuration. Not per-ship.
    /// See Spec 007: In-Flight Targeting.
    /// </summary>
    [CreateAssetMenu(menuName = "VoidHarvest/Targeting Config")]
    public class TargetingConfig : ScriptableObject
    {
        /// <summary>Screen-space padding around target in pixels.</summary>
        public float ReticlePadding = 20f;
        /// <summary>Minimum reticle size in pixels.</summary>
        public float ReticleMinSize = 40f;
        /// <summary>Maximum reticle size in pixels.</summary>
        public float ReticleMaxSize = 300f;
        /// <summary>Progress arc thickness in pixels.</summary>
        public float LockProgressArcWidth = 3f;
        /// <summary>Margin from screen edge for off-screen indicator in pixels.</summary>
        public float OffScreenIndicatorMargin = 30f;
        /// <summary>RenderTexture width for target card viewports.</summary>
        public int ViewportRenderWidth = 140;
        /// <summary>RenderTexture height for target card viewports.</summary>
        public int ViewportRenderHeight = 100;
        /// <summary>Viewport camera field of view.</summary>
        public float ViewportFOV = 30f;
        /// <summary>World-space offset for preview staging area.</summary>
        public Vector3 PreviewStageOffset = new Vector3(0f, -1000f, 0f);
    }
}
