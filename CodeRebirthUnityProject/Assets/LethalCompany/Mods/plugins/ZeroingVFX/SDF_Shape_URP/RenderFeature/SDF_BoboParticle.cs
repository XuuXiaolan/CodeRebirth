using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace Gumou.PostProcessing {
    public class SDF_BoboParticle : ScriptableRendererFeature {
    class RP : ScriptableRenderPass {
        public RenderTargetIdentifier source;
        private RenderTargetHandle tempTex1;
        private RenderTargetHandle tempTex2;
        private Material material = null;
        public Settings settings = null;
        List<ComputeBuffer> buffersToDispose = new List<ComputeBuffer>();

        public RP() {
            Shader shader = Shader.Find("Gumou/RF/SDF_Bobo");
            if (shader == null) return;

            //material = new Material(shader);
            material = CoreUtils.CreateEngineMaterial(shader);
            tempTex1.Init("tex1");
            tempTex2.Init("tex2");
        }

        public override void Configure(CommandBuffer cmd, RenderTextureDescriptor cameraTextureDescriptor) {
            int width = Mathf.RoundToInt((float) cameraTextureDescriptor.width * settings.Scale);
            int height = Mathf.RoundToInt((float) cameraTextureDescriptor.height * settings.Scale);
            cmd.GetTemporaryRT(tempTex1.id, width, height);
            cmd.GetTemporaryRT(tempTex2.id, cameraTextureDescriptor);
            // cmd.Clear();
        }

        public override void Execute(ScriptableRenderContext context, ref RenderingData renderingData) {
            if (material == null) return;
            if (renderingData.cameraData.cameraType != CameraType.Game && renderingData.cameraData.cameraType != CameraType.SceneView) return;
            //
            foreach (var buffer in buffersToDispose) {
                buffer.Dispose();
            }

            buffersToDispose.Clear();
            if (ParticleSdfVolume.S == null) {
                return;
            }

            var camera = renderingData.cameraData.camera;
            CommandBuffer cmd = CommandBufferPool.Get("SDF_BoboParticle");
            cmd.Clear();

            cmd.Blit(source, tempTex2.Identifier());
            cmd.SetGlobalTexture("_SourceTex", tempTex2.id);

            SetVolume(ParticleSdfVolume.S.GetShapeDatas(), cmd);
            Matrix4x4 IVP = (GL.GetGPUProjectionMatrix(camera.projectionMatrix, false)
                             * camera.worldToCameraMatrix).inverse;
            material.SetMatrix(SDF_BoboParticle.IVP, IVP);
            material.SetInt(SDF_BoboParticle._MaxStep, settings.MaxStep);
            material.SetFloat(SDF_BoboParticle._Epsilon, settings.Epsilon);
            material.SetColor(SDF_BoboParticle._Color, settings.color);
            material.SetColor(SDF_BoboParticle._Specular, settings.Specular);
            material.SetFloat(SDF_BoboParticle._Reflection, settings.Reflection);
            material.SetFloat(SDF_BoboParticle._Warp, settings.Warp);
            material.SetFloat(SDF_BoboParticle._WarpScale, settings.WarpScale);
            material.SetFloat(SDF_BoboParticle._DepthBlend, settings.DepthBlend);
            if (settings._matcap == null) {
                material.SetTexture(SDF_BoboParticle._MatcapTex, Texture2D.blackTexture);
            }
            else {
                material.SetTexture(SDF_BoboParticle._MatcapTex, settings._matcap);
            }

            //
            cmd.Blit(null, tempTex1.Identifier(), material, 0);
            cmd.Blit(tempTex1.Identifier(), source, material, 1);

            //
            context.ExecuteCommandBuffer(cmd);
            cmd.Clear();
            CommandBufferPool.Release(cmd);
        }

        void SetVolume(SdfShape.ShapeData[] shapeDatas, CommandBuffer cmd) {
            if (shapeDatas == null || shapeDatas.Length == 0) {
                return;
            }

            ComputeBuffer shapeBuffer = new ComputeBuffer(shapeDatas.Length, SdfShape.ShapeData.GetSize());
            shapeBuffer.SetData(shapeDatas);
            cmd.SetGlobalBuffer("shapes", shapeBuffer);
            cmd.SetGlobalInt("numShapes", shapeDatas.Length);
            buffersToDispose.Add(shapeBuffer);
        }

        public override void OnCameraCleanup(CommandBuffer cmd) {
            base.OnCameraCleanup(cmd);
            cmd.ReleaseTemporaryRT(tempTex1.id);
            cmd.ReleaseTemporaryRT(tempTex2.id);
        }
    }

    [System.Serializable]
    public class Settings {
        public Color color = new Color(0.7490196f, 0.8901961f, 1, 1);
        public int MaxStep = 50;
        public float Epsilon = 0.01f;
        [Range(0.1f, 1)] public float Scale = 1;
        public float Warp = 1;
        public float WarpScale = 2;
        public float DepthBlend = 0.2f;

        [Header("render"), ColorUsage(true, true)]
        public Color Specular;
        public Texture _matcap;
        [Range(0, 1)] public float Reflection = 0.7f;
    }

    private RP _pass;
    public Settings settings;
    private static readonly int IVP = Shader.PropertyToID("IVP");
    private static readonly int _MaxStep = Shader.PropertyToID("_MaxStep");
    private static readonly int _Epsilon = Shader.PropertyToID("_Epsilon");
    private static readonly int _Color = Shader.PropertyToID("_Color");
    private static readonly int _MatcapTex = Shader.PropertyToID("_MatcapTex");
    private static readonly int _Specular = Shader.PropertyToID("_Specular");
    private static readonly int _Reflection = Shader.PropertyToID("_Reflection");
    private static readonly int _Warp = Shader.PropertyToID("_Warp");
    private static readonly int _WarpScale = Shader.PropertyToID("_WarpScale");
    private static readonly int _DepthBlend = Shader.PropertyToID("_DepthBlend");

    public override void Create() {
        _pass = new RP();
        _pass.settings = settings;
        _pass.renderPassEvent = RenderPassEvent.AfterRenderingTransparents;
    }

    public override void AddRenderPasses(ScriptableRenderer renderer, ref RenderingData renderingData) {
        _pass.source = renderer.cameraColorTarget;
        _pass.settings = settings;
        renderer.EnqueuePass(_pass);
    }
}
}
