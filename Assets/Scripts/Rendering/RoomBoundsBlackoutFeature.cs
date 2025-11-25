#if UNITY_RENDER_PIPELINE_UNIVERSAL || UNITY_EDITOR
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.Rendering.RenderGraphModule; // For RenderGraph types

// Minimal custom Renderer Feature to blackout pixels outside room bounds.
public class RoomBoundsBlackoutFeature : ScriptableRendererFeature
{
    [System.Serializable]
    public class PassSettings
    {
        public Material blackoutMaterial; // assign material that uses Hidden/RoomBoundsBlackout shader
        public RenderPassEvent injectionPoint = RenderPassEvent.AfterRenderingPostProcessing;
    }

    public PassSettings settings = new PassSettings();

    class BlackoutPass : ScriptableRenderPass
    {
        private Material _material;
        public void Setup(Material mat, RenderPassEvent evt)
        {
            _material = mat;
            renderPassEvent = evt;
        }

        public override void Execute(ScriptableRenderContext context, ref RenderingData renderingData)
        {
            if (_material == null || !RoomBoundsBlackoutContext.Enabled) return;

            // Update material parameters from global context
            _material.SetVector("_RoomMin", RoomBoundsBlackoutContext.RoomMin);
            _material.SetVector("_RoomMax", RoomBoundsBlackoutContext.RoomMax);
            _material.SetFloat("_SoftMargin", RoomBoundsBlackoutContext.SoftMargin);

            var desc = renderingData.cameraData.cameraTargetDescriptor;
            int tempID = Shader.PropertyToID("_RoomBlackoutTemp");

            var renderer = renderingData.cameraData.renderer;
            var sourceHandle = renderer.cameraColorTargetHandle; // RTHandle
            var sourceRT = sourceHandle.nameID; // RenderTargetIdentifier

            CommandBuffer cmd = CommandBufferPool.Get();
            cmd.GetTemporaryRT(tempID, desc); // temp RT
            cmd.Blit(sourceRT, tempID, _material, 0); // apply blackout to temp
            cmd.Blit(tempID, sourceRT); // copy back to camera color target
            cmd.ReleaseTemporaryRT(tempID);
            context.ExecuteCommandBuffer(cmd);
            CommandBufferPool.Release(cmd);
        }

        // Stub for RenderGraph path. Effect currently handled only in Execute() (compatibility mode).
        // Implementing a RenderGraph version would require adding a raster pass and sampling camera color.
        public override void RecordRenderGraph(RenderGraph renderGraph, ContextContainer frameData)
        {
            // No-op: rely on compatibility mode. If RenderGraph is enabled, enable compatibility mode or implement pass.
        }
    }

    BlackoutPass _pass;

    public override void Create()
    {
        _pass = new BlackoutPass();
    }

    public override void AddRenderPasses(ScriptableRenderer renderer, ref RenderingData renderingData)
    {
        if (_pass == null) return;
        _pass.Setup(settings.blackoutMaterial, settings.injectionPoint);
        renderer.EnqueuePass(_pass);
    }
}
#else
using UnityEngine;
// Stub when URP not present.
public class RoomBoundsBlackoutFeature : ScriptableObject { }
#endif
