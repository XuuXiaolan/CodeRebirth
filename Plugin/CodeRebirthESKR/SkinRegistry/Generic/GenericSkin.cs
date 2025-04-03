using AntlerShed.SkinRegistry;
using CodeRebirthESKR.Misc;
using UnityEngine;

namespace CodeRebirthESKR.SkinRegistry.Generic;
[CreateAssetMenu(fileName = "GenericSkin", menuName = "XSkins/GenericSkin", order = 1)]
public class GenericSkin : BaseSkin
{
    [Header("Materials")]
    [SerializeField] private MaterialAction[] allMaterialsAction;
    [SerializeField] private MaterialAction mapDotMaterialAction;

    [Space(10)]

    [Header("Audio")]
    [SerializeField] private AudioAction deathAudioAction;
    [SerializeField] private AudioAction hitBodyAudioAction;
    [Space(10)]

    [Header("Particles")]
    [Space(10)]

    [Header("Armature Attachments")]
    [SerializeField] private ArmatureAttachment[] genericAttachments = [];

    [Header("Misc")]
    [Tooltip("Ensure renderers are set before skinning, this might be necessary for some enemies.")]
    [SerializeField] private bool ensureRenderersAreSet = false;

    public MaterialAction[] AllMaterialsAction => allMaterialsAction;
    public MaterialAction MapDotMaterialAction => mapDotMaterialAction;
    public AudioAction HitBodyAudioAction => hitBodyAudioAction;
    public AudioAction DeathAudioAction => deathAudioAction;
    public ArmatureAttachment[] GenericAttachments => genericAttachments;
    public bool EnsureRenderersAreSet => ensureRenderersAreSet;

    public override Skinner CreateSkinner()
    {
        return new GenericSkinner(this);
    }
}