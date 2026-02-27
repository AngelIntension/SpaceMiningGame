using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace VoidHarvest.Features.HUD.Views
{
    /// <summary>
    /// URP Renderer Feature for selected asteroid outline effect.
    /// Inverted-hull technique: second pass with front-face culling and vertex extrusion.
    /// 2px white outline per spec. See MVP-03: Target selection highlight.
    /// </summary>
    public sealed class SelectionOutlineFeature : ScriptableRendererFeature
    {
        /// <summary>
        /// Serializable settings for the selection outline renderer feature. See MVP-03: Target selection highlight.
        /// </summary>
        [System.Serializable]
        public class OutlineSettings
        {
            /// <summary>Material with front-face culling and vertex extrusion shader. See MVP-03.</summary>
            public Material outlineMaterial;
            /// <summary>Outline width in world-space units (2px equivalent). See MVP-03.</summary>
            public float outlineWidth = 0.02f;
            /// <summary>Outline color, default white per spec. See MVP-03.</summary>
            public Color outlineColor = Color.white;
            /// <summary>Layer mask filtering which objects receive the outline. See MVP-03.</summary>
            public LayerMask selectionLayer;
            /// <summary>Render pass timing for the outline effect. See MVP-03.</summary>
            public RenderPassEvent renderPassEvent = RenderPassEvent.AfterRenderingOpaques;
        }

        [SerializeField] private OutlineSettings settings = new();
        private SelectionOutlinePass _outlinePass;

        public override void Create()
        {
            _outlinePass = new SelectionOutlinePass(settings);
            _outlinePass.renderPassEvent = settings.renderPassEvent;
        }

        public override void AddRenderPasses(ScriptableRenderer renderer, ref RenderingData renderingData)
        {
            if (settings.outlineMaterial == null) return;
            renderer.EnqueuePass(_outlinePass);
        }

        protected override void Dispose(bool disposing)
        {
            _outlinePass?.Dispose();
        }
    }

    internal sealed class SelectionOutlinePass : ScriptableRenderPass
    {
        private readonly SelectionOutlineFeature.OutlineSettings _settings;
        private FilteringSettings _filteringSettings;

        public SelectionOutlinePass(SelectionOutlineFeature.OutlineSettings settings)
        {
            _settings = settings;
            _filteringSettings = new FilteringSettings(RenderQueueRange.opaque, settings.selectionLayer);
        }

        public override void Execute(ScriptableRenderContext context, ref RenderingData renderingData)
        {
            var cmd = CommandBufferPool.Get("SelectionOutline");

            // Set outline material properties
            _settings.outlineMaterial.SetFloat("_OutlineWidth", _settings.outlineWidth);
            _settings.outlineMaterial.SetColor("_OutlineColor", _settings.outlineColor);

            // Draw with outline material (front-face culled, vertex-extruded)
            var drawingSettings = CreateDrawingSettings(
                new ShaderTagId("UniversalForward"),
                ref renderingData,
                SortingCriteria.CommonOpaque);
            drawingSettings.overrideMaterial = _settings.outlineMaterial;
            drawingSettings.overrideMaterialPassIndex = 0;

            context.DrawRenderers(renderingData.cullResults, ref drawingSettings, ref _filteringSettings);

            context.ExecuteCommandBuffer(cmd);
            CommandBufferPool.Release(cmd);
        }

        public void Dispose() { }
    }
}
