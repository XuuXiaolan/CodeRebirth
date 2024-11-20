using UnityEngine;
using UnityEngine.Rendering.HighDefinition;
using UnityEngine.Rendering;
using System.Collections.Generic;
using CodeRebirth.src.Util;

public class SeeThroughCustomPass : CustomPass
{
    [SerializeField]
    public Material seeThroughMaterial = new(CodeRebirthUtils.Instance.WireframeMaterial);
    public LayerMask seeThroughLayer;
    public float maxVisibilityDistance = 20f;

    [SerializeField]
    readonly Shader stencilShader = CodeRebirthUtils.Instance.SeeThroughShader;

    Material? stencilMaterial = null;

    ShaderTagId[] shaderTags = [];

    protected override bool executeInSceneView => true;

    public void ConfigureMaterial(Color edgeColor, Color fillColor, float thickness)
    {
        seeThroughMaterial.SetColor("_EdgeColor", edgeColor);
        seeThroughMaterial.SetColor("_MainColor", fillColor);
        seeThroughMaterial.SetFloat("_WireframeVal", thickness);
    }

    protected override void Setup(ScriptableRenderContext renderContext, CommandBuffer cmd)
    {
        stencilMaterial = CoreUtils.CreateEngineMaterial(stencilShader);

        shaderTags =
        [
            new ShaderTagId("Forward"),
            new ShaderTagId("ForwardOnly"),
            new ShaderTagId("SRPDefaultUnlit"),
            new ShaderTagId("FirstPass"),
        ];
    }

    protected override void Execute(CustomPassContext ctx)
    {
        // We first render objects into the user stencil bit 0, this will allow us to detect
        // if the object is behind another object.
        if(stencilMaterial == null)
            stencilMaterial = CoreUtils.CreateEngineMaterial(stencilShader);

        stencilMaterial.SetInt("_StencilWriteMask", (int)UserStencilUsage.UserBit0);
        seeThroughMaterial.SetFloat("_MaxVisibilityDistance", maxVisibilityDistance);

        RenderObjects(ctx.renderContext, ctx.cmd, stencilMaterial, 0, CompareFunction.LessEqual, ctx.cullingResults, ctx.hdCamera);

        // Then we render the objects that are behind walls using the stencil buffer with Greater Equal ZTest:
        StencilState seeThroughStencil = new(
            enabled: true,
            readMask: (byte)UserStencilUsage.UserBit0,
            compareFunction: CompareFunction.Equal
        );

        RenderObjects(ctx.renderContext, ctx.cmd, seeThroughMaterial, seeThroughMaterial.FindPass("ForwardOnly"), CompareFunction.GreaterEqual, ctx.cullingResults, ctx.hdCamera, seeThroughStencil);
    }

    public override IEnumerable<Material> RegisterMaterialForInspector() { yield return seeThroughMaterial; }

    void RenderObjects(ScriptableRenderContext renderContext, CommandBuffer cmd, Material overrideMaterial, int passIndex, CompareFunction depthCompare, CullingResults cullingResult, HDCamera hdCamera, StencilState? overrideStencil = null)
    {
        var result = new UnityEngine.Rendering.RendererUtils.RendererListDesc(shaderTags, cullingResult, hdCamera.camera)
        {
            rendererConfiguration = PerObjectData.None,
            renderQueueRange = RenderQueueRange.all,
            sortingCriteria = SortingCriteria.BackToFront,
            excludeObjectMotionVectors = false,
            overrideMaterial = overrideMaterial,
            overrideMaterialPassIndex = passIndex,
            layerMask = seeThroughLayer,
            stateBlock = new RenderStateBlock(RenderStateMask.Depth) { depthState = new DepthState(writeEnabled: true, compareFunction: depthCompare) },
        };

        if (overrideStencil != null)
        {
            var block = result.stateBlock.Value;
            block.mask |= RenderStateMask.Stencil;
            block.stencilState = overrideStencil.Value;
            result.stateBlock = block;
        }

        CoreUtils.DrawRendererList(renderContext, cmd, renderContext.CreateRendererList(result));
    }

    protected override void Cleanup()
    {
        // Cleanup code
    }
}