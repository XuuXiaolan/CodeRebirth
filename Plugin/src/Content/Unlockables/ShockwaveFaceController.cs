using System.Collections;
using CodeRebirth.src.Util.Extensions;
using UnityEngine;
using static CodeRebirth.src.Content.Unlockables.ShockwaveGalAI;

namespace CodeRebirth.src.Content.Unlockables;
public class ShockwaveFaceController : MonoBehaviour
{
    public ShockwaveGalAI ShockwaveGalAI = null!;
    private SkinnedMeshRenderer FaceSkinnedMeshRenderer = null!;
    private Renderer FaceRenderer = null!;
    public float GlitchDuration = 0.1f;
    public float GlitchFrequency = 2f;
    
    private float glitchTimer;
    private bool isGlitching = false;
    private Coroutine? glitchCoroutine = null;
    private Coroutine? modeCoroutine = null;
    private System.Random controllerRandom = new System.Random();
    public enum RobotMode
    {
        Normal = 0,
        Combat = 1
    }

    [SerializeField]
    private RobotMode currentMode = RobotMode.Normal;

    private readonly Color greenColor = new Color(0f, 0.671f, 0.027f);
    private readonly Color lightGreenColor = new Color(0.5f, 1f, 0.5f);
    private readonly Color redColor = new Color(0.859f, 0.016f, 0f);
    private readonly Color lightRedColor = new Color(1f, 0.5f, 0.5f);

    private Material[] originalMaterials;

    private void Start()
    {
        controllerRandom = new System.Random(StartOfRound.Instance.randomMapSeed + 32);
        FaceSkinnedMeshRenderer = ShockwaveGalAI.FaceSkinnedMeshRenderer;
        FaceRenderer = ShockwaveGalAI.FaceRenderer;
        glitchTimer = controllerRandom.NextFloat(1f / GlitchFrequency, 3f / GlitchFrequency);
        SetFaceState(ShockwaveGalAI.galEmotion, 100f);
        SetMode(currentMode);

        originalMaterials = FaceRenderer.materials;
    }

    private void Update()
    {
        glitchTimer -= Time.deltaTime;
        if (glitchTimer <= 0)
        {
            if (glitchCoroutine != null)
            {
                StopCoroutine(glitchCoroutine);
            }
            glitchCoroutine = StartCoroutine(GlitchEffect());
            glitchTimer = controllerRandom.NextFloat(1f / GlitchFrequency, 3f / GlitchFrequency);
        }
    }

    private IEnumerator GlitchEffect()
    {
        isGlitching = true;

        FaceSkinnedMeshRenderer.SetBlendShapeWeight((int)ShockwaveGalAI.galEmotion, 0f);

        int glitchShapeKeyIndex = controllerRandom.NextInt(0, FaceSkinnedMeshRenderer.sharedMesh.blendShapeCount - 1);
        float glitchWeight = controllerRandom.NextFloat(0f, 100f);
        FaceSkinnedMeshRenderer.SetBlendShapeWeight(glitchShapeKeyIndex, glitchWeight);

        yield return new WaitForSeconds(GlitchDuration);

        FaceSkinnedMeshRenderer.SetBlendShapeWeight(glitchShapeKeyIndex, 0f);
        FaceSkinnedMeshRenderer.SetBlendShapeWeight((int)ShockwaveGalAI.galEmotion, 100f);

        isGlitching = false;
        glitchCoroutine = null;
    }

    public void SetFaceState(Emotion faceState, float weight)
    {
        if (glitchCoroutine != null)
        {
            StopCoroutine(glitchCoroutine);
            glitchCoroutine = null;
        }

        ResetFace();
        
        //set the current face state
        ShockwaveGalAI.galEmotion = faceState;
        FaceSkinnedMeshRenderer.SetBlendShapeWeight((int)faceState, Mathf.Clamp(weight, 0f, 100f));
    }

    public void SetMode(RobotMode mode)
    {
        if (modeCoroutine != null)
        {
            StopCoroutine(modeCoroutine);
            modeCoroutine = null;
        }
        modeCoroutine = StartCoroutine(BlinkModeTransition(mode));
    }

    private IEnumerator BlinkModeTransition(RobotMode mode)
    {
        int blinkCount = 4;
        float blinkDuration = 0.2f;

        for (int i = 0; i < blinkCount; i++)
        {
            Color blinkEmissionColor = (i % 2 == 0) ? greenColor : redColor;
            Color blinkFaceColor = (i % 2 == 0) ? lightGreenColor : lightRedColor;
            ApplyEmissionColor(blinkEmissionColor);
            ApplyFaceColor(blinkFaceColor);
            yield return new WaitForSeconds(blinkDuration);
        }

        currentMode = mode;
        Color finalEmissionColor = (mode == RobotMode.Combat) ? redColor : greenColor;
        Color finalFaceColor = (mode == RobotMode.Combat) ? lightRedColor : lightGreenColor;
        ApplyEmissionColor(finalEmissionColor);
        ApplyFaceColor(finalFaceColor);
        modeCoroutine = null;
    }

    private void ApplyEmissionColor(Color emissionColor)
    {
        var materials = FaceRenderer.materials;
        materials[0].SetColor("_EmissiveColor", emissionColor);
        materials[2].SetColor("_EmissiveColor", emissionColor);
        FaceRenderer.materials = materials;
    }

    private void ApplyFaceColor(Color faceColor)
    {
        var materials = FaceSkinnedMeshRenderer.materials;
        materials[0].SetColor("_EmissiveColor", faceColor);
        FaceSkinnedMeshRenderer.materials = materials;
    }

    public void ResetFace()
    {
        foreach (Emotion faceState in System.Enum.GetValues(typeof(Emotion)))
        {
            FaceSkinnedMeshRenderer.SetBlendShapeWeight((int)faceState, 0f);
        }
    }
    
    private void OnDisable()
    {
        //reset
        FaceSkinnedMeshRenderer.SetBlendShapeWeight((int)Emotion.Happy, 0f);
        FaceRenderer.materials = originalMaterials;
    }
}