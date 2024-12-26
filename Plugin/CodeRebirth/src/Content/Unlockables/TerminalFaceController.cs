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
        SetFaceState((Emotion)shapeKeyIndex, 100f);

        float Duration = controllerRandom.NextFloat(0.5f, 1f);
        yield return new WaitForSeconds(Duration);

        SetFaceState(TerminalGalAI.galEmotion, 100f);
    }

    public void SetFaceState(Emotion faceState, float weight)
    {
        ResetFace();
        
        //set the current face state
        Plugin.ExtendedLogging($"{TerminalGalAI} setting face state to: {faceState}");
        if (faceState != Emotion.Winky) TerminalGalAI.galEmotion = faceState;
        if ((int)faceState == -1) return;
        FaceSkinnedMeshRenderer.SetBlendShapeWeight((int)faceState, Mathf.Clamp(weight, 0f, 100f));
    }

    public void ResetFace()
    {
        foreach (Emotion faceState in System.Enum.GetValues(typeof(Emotion)))
        {
            if ((int)faceState == -1) continue;
            FaceSkinnedMeshRenderer.SetBlendShapeWeight((int)faceState, 0f);
        }
    }
}