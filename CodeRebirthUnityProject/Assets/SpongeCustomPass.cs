using UnityEngine;
using UnityEngine.Experimental.Rendering;
using UnityEngine.Rendering;
using UnityEngine.Rendering.HighDefinition;

namespace Scoops.rendering
{
    class SpongeNewCustomPass : CustomPass
    {
        public static Material posterizationMaterial;
        public static Shader posterizationShader;
        public static RTHandle posterizationRT;

        protected override void Setup(ScriptableRenderContext renderContext, CommandBuffer cmd)
        {
            posterizationShader = (Shader)Shader.Find("FullScreen/SpongePosterizeNew");

            posterizationRT = RTHandles.Alloc(
                    Vector2.one, TextureXR.slices, dimension: TextureXR.dimension,
                    colorFormat: GraphicsFormat.B10G11R11_UFloatPack32,
                    useDynamicScale: true, name: "Posterization Buffer"
                );

            posterizationMaterial = CoreUtils.CreateEngineMaterial(posterizationShader);
        }

        protected override void Execute(CustomPassContext ctx)
        {
            ctx.propertyBlock.SetTexture("_SpongeCameraColorBuffer", ctx.cameraColorBuffer, RenderTextureSubElement.Color);
            ctx.propertyBlock.SetFloat("_OutlineThickness", 0.001f);
            ctx.propertyBlock.SetFloat("_DepthThreshold", 0.4f);
            ctx.propertyBlock.SetFloat("_DepthCurve", 0.4f);
            ctx.propertyBlock.SetFloat("_DepthStrength", 6f);
            ctx.propertyBlock.SetFloat("_ColorThreshold", 0.47f);
            ctx.propertyBlock.SetFloat("_ColorCurve", 2.94f);
            ctx.propertyBlock.SetFloat("_ColorStrength", 0.65f);

            CoreUtils.SetRenderTarget(ctx.cmd, posterizationRT, ClearFlag.All);
            CoreUtils.DrawFullScreen(ctx.cmd, posterizationMaterial, ctx.propertyBlock, posterizationMaterial.FindPass("ReadColor"));

            ctx.propertyBlock.SetTexture("_PosterizationBuffer", posterizationRT);

            CoreUtils.SetRenderTarget(ctx.cmd, ctx.cameraColorBuffer, ClearFlag.None);
            CoreUtils.DrawFullScreen(ctx.cmd, posterizationMaterial, ctx.propertyBlock, posterizationMaterial.FindPass("WriteColor"));
        }

        protected override void Cleanup()
        {
            CoreUtils.Destroy(posterizationMaterial);
            posterizationRT.Release();
        }
    }
}
