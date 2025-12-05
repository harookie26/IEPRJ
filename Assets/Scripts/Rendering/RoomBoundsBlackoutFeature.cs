using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.Rendering.RenderGraphModule;

public class RoomBoundsBlackoutFeature : ScriptableRendererFeature
{
    [System.Serializable]
    public class PassSettings
    {
        public Material blackoutMaterial;
        public RenderPassEvent injectionPoint = RenderPassEvent.AfterRenderingPostProcessing;
    }

    // This data was causing the mismatch (60 vs 64 bytes)
    public PassSettings settings = new PassSettings();

    class BlackoutPass : ScriptableRenderPass
    {
        private Material _material;

        public void Setup(Material mat, RenderPassEvent evt)
        {
            _material = mat;
            renderPassEvent = evt;
        }

        // Compatibility Mode Execute (still needed for some URP 6 contexts)
        public override void Execute(ScriptableRenderContext context, ref RenderingData renderingData)
        {
            if (_material == null || !RoomBoundsBlackoutContext.Enabled) return;

            // Safe null checks for the context data
            var roomMin = RoomBoundsBlackoutContext.RoomMin;
            var roomMax = RoomBoundsBlackoutContext.RoomMax;

            _material.SetVector("_RoomMin", roomMin);
            _material.SetVector("_RoomMax", roomMax);
            _material.SetFloat("_SoftMargin", RoomBoundsBlackoutContext.SoftMargin);

            var desc = renderingData.cameraData.cameraTargetDescriptor;
            int tempID = Shader.PropertyToID("_RoomBlackoutTemp");

            var renderer = renderingData.cameraData.renderer;
            var sourceHandle = renderer.cameraColorTargetHandle;

            CommandBuffer cmd = CommandBufferPool.Get("RoomBlackout");
            cmd.GetTemporaryRT(tempID, desc);
            cmd.Blit(sourceHandle, tempID, _material, 0);
            cmd.Blit(tempID, sourceHandle);
            cmd.ReleaseTemporaryRT(tempID);

            context.ExecuteCommandBuffer(cmd);
            CommandBufferPool.Release(cmd);
        }

        public override void RecordRenderGraph(RenderGraph renderGraph, ContextContainer frameData)
        {
            // Keep this empty if you are relying on Compatibility Mode (Execute)
        }
    }

    BlackoutPass _pass;

    public override void Create()
    {
        _pass = new BlackoutPass();
    }

    public override void AddRenderPasses(ScriptableRenderer renderer, ref RenderingData renderingData)
    {
        if (_pass != null && settings.blackoutMaterial != null)
        {
            _pass.Setup(settings.blackoutMaterial, settings.injectionPoint);
            renderer.EnqueuePass(_pass);
        }
    }
}