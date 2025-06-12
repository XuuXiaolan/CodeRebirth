using System.Runtime.InteropServices;
using UnityEngine;
using UnityEngine.VFX;
using System;

namespace CodeRebirth.src.MiscScripts.DissolveEffect;
[Serializable]
public class VFXUniformMeshBaker
{
    public static string GraphicsBufferName = "UniformMeshBuffer";
    public int SampleCount
    {
        get => Mathf.Min((int)(_sampleCount * SampleCountMultiplier), 100000);
    }

    [Tooltip("Amount of points from which particles would be spawned.")]
    [SerializeField]
    private int _sampleCount = 2048;

    [Range(0.01f, 10f), SerializeField]
    [Tooltip("Multiply sample count by this value to control density of the particles. Keep this as low as possible.")]
    public float SampleCountMultiplier = 1f;

    [SerializeField]
    private TriangleSampling[] m_BakedSampling;

    private GraphicsBuffer m_Buffer;

    private void ComputeBakedSampling(VisualEffect visualEffect, Mesh mesh)
    {
        if (visualEffect == null)
        {
            Debug.LogWarning("UniformBaker expects a VisualEffect on the shared game object.");
            return;
        }

        if (!visualEffect.HasGraphicsBuffer(GraphicsBufferName))
        {
            Debug.LogWarningFormat("Graphics Buffer property '{0}' is invalid.", GraphicsBufferName);
            return;
        }

        var meshData = VFXMeshSamplingHelper.ComputeDataCache(mesh);

        _sampleCount = meshData.triangles.Length;

        var rand = new System.Random(123); // use random number as seed
        m_BakedSampling = new TriangleSampling[SampleCount];
        for (int i = 0; i < SampleCount; ++i)
        {
            m_BakedSampling[i] = VFXMeshSamplingHelper.GetNextSampling(meshData, rand);
        }
    }
    private void UpdateGraphicsBuffer()
    {
        if (m_BakedSampling == null) return;

        if (SampleCount != m_BakedSampling.Length)
        {
            //Debug.LogErrorFormat("The length of baked data mismatches with sample count : {0} vs {1}", SampleCount, m_BakedSampling.Length);
            return;
        }

        if (m_Buffer != null)
        {
            m_Buffer.Release();
            m_Buffer = null;
        }

        m_Buffer = new GraphicsBuffer(GraphicsBuffer.Target.Structured, SampleCount, Marshal.SizeOf(typeof(TriangleSampling)));
        m_Buffer.SetData(m_BakedSampling);
    }
    private void BindGraphicsBuffer(VisualEffect vfx)
    {
        if (vfx.HasGraphicsBuffer(GraphicsBufferName)) vfx.SetGraphicsBuffer(GraphicsBufferName, m_Buffer);
    }

    private Mesh RendererToMesh(Renderer meshRenderer)
    {
        Mesh mesh;

        if (meshRenderer is SkinnedMeshRenderer)
        {
            mesh = (meshRenderer as SkinnedMeshRenderer).sharedMesh;
        }
        else
        {
            mesh = (meshRenderer as MeshRenderer).GetComponent<MeshFilter>().sharedMesh;
        }

        return mesh;
    }

    public void Update(VisualEffect visualEffect, Renderer renderer)
    {
        if (m_BakedSampling == null || m_BakedSampling.Length < 1)
        {
            Bake(visualEffect, renderer);
        }
        else if (m_Buffer == null)
        {
            SetGraphicsBuffer(visualEffect);
        }
    }

    public void OnDisable()
    {
        if (m_Buffer != null)
        {
            m_Buffer.Release();
            m_Buffer = null;
        }
    }

    public void Bake(VisualEffect visualEffect, Renderer renderer)
    {
        ComputeBakedSampling(visualEffect, RendererToMesh(renderer));
        UpdateGraphicsBuffer();
        BindGraphicsBuffer(visualEffect);
    }

    public void SetGraphicsBuffer(VisualEffect visualEffect)
    {
        UpdateGraphicsBuffer();
        BindGraphicsBuffer(visualEffect);
    }
}