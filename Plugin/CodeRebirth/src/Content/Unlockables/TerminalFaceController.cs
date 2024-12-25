using System.Collections;
using CodeRebirth.src.Util.Extensions;
using UnityEngine;
using static CodeRebirth.src.Content.Unlockables.TerminalGalAI;

namespace CodeRebirth.src.Content.Unlockables;
public class TerminalFaceController : MonoBehaviour
{
    public TerminalGalAI TerminalGalAI = null!;
    private SkinnedMeshRenderer FaceSkinnedMeshRenderer = null!;
    
    [HideInInspector] public float glitchTimer;
    [HideInInspector] public Coroutine? TemporarySwitchCoroutine = null;
    [HideInInspector] public System.Random controllerRandom = new();

    public void OnEnable()
    {
        controllerRandom = new System.Random(StartOfRound.Instance.randomMapSeed + 32);
        FaceSkinnedMeshRenderer = TerminalGalAI.FaceSkinnedMeshRenderer;
        SetFaceState(TerminalGalAI.galEmotion, 100f);
    }

    private void Update()
    {
        GlitchUpdate();
    }

    private void GlitchUpdate()
    {
        glitchTimer -= Time.deltaTime;
        if (glitchTimer <= 0)
        {
            if (TemporarySwitchCoroutine != null)
            {
                StopCoroutine(TemporarySwitchCoroutine);
            }
            TemporarySwitchCoroutine = StartCoroutine(TemporarySwitchEffect((int)Emotion.Winky));
            glitchTimer = controllerRandom.NextFloat(4f, 8f);
        }
    }

    public IEnumerator TemporarySwitchEffect(int shapeKeyIndex)
    {
        FaceSkinnedMeshRenderer.SetBlendShapeWeight((int)TerminalGalAI.galEmotion, 0f);

        int ShapeKeyIndex = shapeKeyIndex; // todo: set this to be the wink and not a random index.
        float Weight = controllerRandom.NextFloat(50f, 100f);
        FaceSkinnedMeshRenderer.SetBlendShapeWeight(ShapeKeyIndex, Weight);

        float Duration = controllerRandom.NextFloat(0.5f, 1f);
        yield return new WaitForSeconds(Duration);

        FaceSkinnedMeshRenderer.SetBlendShapeWeight(ShapeKeyIndex, 0f);
        FaceSkinnedMeshRenderer.SetBlendShapeWeight((int)TerminalGalAI.galEmotion, 100f);

        TemporarySwitchCoroutine = null;
    }

    public void SetFaceState(Emotion faceState, float weight)
    {
        ResetFace();
        
        //set the current face state
        TerminalGalAI.galEmotion = faceState;
        FaceSkinnedMeshRenderer.SetBlendShapeWeight((int)faceState, Mathf.Clamp(weight, 0f, 100f));
    }

    public void ResetFace()
    {
        foreach (Emotion faceState in System.Enum.GetValues(typeof(Emotion)))
        {
            FaceSkinnedMeshRenderer.SetBlendShapeWeight((int)faceState, 0f);
        }
    }
}