using UnityEngine;
using UnityEngine.Experimental.Rendering;
using UnityEngine.Rendering;
using UnityEngine.Rendering.RenderGraphModule;
using UnityEngine.Rendering.Universal;
using static UnityEngine.Rendering.ProfilingSampler;

public class RadialLightRenderer : ScriptableRendererFeature
{
    private class PassData
    {
        public TextureHandle source;
        public Material material;
        public Vector4 playerScreenPos;
    }

    class RadialLightPass : ScriptableRenderPass
    {
        private Material _material;
        private Transform _player;

        public RadialLightPass(Material material, Transform player)
        {
            _material = material;
            _player = player;
            renderPassEvent = RenderPassEvent.AfterRendering;
        }

        public override void RecordRenderGraph(RenderGraph renderGraph, ContextContainer context)
        {
            UniversalResourceData data = context.Get<UniversalResourceData>();
            TextureHandle source = data.activeColorTexture;

            var pass = renderGraph.AddRasterRenderPass<PassData>(
                "RadialLightPass",
                out var passData
            );

            passData.source = source;
            passData.material = _material;
            passData.playerScreenPos = Camera.main != null && _player != null
                ? Camera.main.WorldToViewportPoint(_player.position)
                : Vector4.zero;

            pass.SetRenderFunc((PassData data, RasterGraphContext ctx) =>
            {
                if (data.material == null) return;

                data.material.SetVector("_PlayerPos", data.playerScreenPos);

                // Built-in full-screen triangle mesh
                var mesh = RenderingUtils.fullscreenMesh;
                ctx.cmd.DrawMesh(mesh, Matrix4x4.identity, data.material, 0, 0);
            });


        }

    }

    [SerializeField] Shader radialLightShader;
    public string playerTag = "Player";

    Material _material;
    RadialLightPass _pass;
    Transform _player;

    public override void Create()
    {
        if (radialLightShader != null)
            _material = CoreUtils.CreateEngineMaterial(radialLightShader);

        _pass = new RadialLightPass(_material, null)
        {
            renderPassEvent = RenderPassEvent.AfterRendering
        };
    }

    public override void AddRenderPasses(ScriptableRenderer renderer, ref RenderingData renderingData)
    {
        if (_player == null)
        {
            var playerGO = GameObject.FindGameObjectWithTag(playerTag);
            if (playerGO != null)
                _player = playerGO.transform;

            _pass = new RadialLightPass(_material, _player)
            {
                renderPassEvent = RenderPassEvent.AfterRendering
            };
        }

        renderer.EnqueuePass(_pass);
    }
}
