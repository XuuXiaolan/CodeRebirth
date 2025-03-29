using AntlerShed.SkinRegistry;
using CodeRebirthESKR.Misc;
using UnityEngine;

namespace CodeRebirthESKR.SkinRegistry.Jimothy;
[CreateAssetMenu(fileName = "JimothySkinDefinition", menuName = "CodeRebirthESKR/JimothySkinDefinition", order = 1)]
public class JimothySkin : BaseSkin
{
    [Header("Materials")]
    [SerializeField] private MaterialAction smallPartsMaterialAction;
    [SerializeField] private MaterialAction forkliftMaterialAction;
    [SerializeField] private MaterialAction lightMaterialAction;
    [Space(10)]

    [Header("Audio")]
    [SerializeField] private AudioAction dumpAudioAction;
    [SerializeField] private AudioAction hitJimothyAudioAction;
    [SerializeField] private AudioAction jimHonkVoiceAudioAction;
    [SerializeField] private AudioAction pickUpHazardAudioAction;
    [Space(10)]

    [Header("Particles")]
    [Space(10)]

    [Header("Armature Attachments")]
    [SerializeField] private ArmatureAttachment[] jimothyAttachments = [];
    [SerializeField] private ArmatureAttachment[] machineAttachments = [];

    public MaterialAction SmallPartsMaterialAction => smallPartsMaterialAction;
    public MaterialAction ForkliftMaterialAction => forkliftMaterialAction;
    public MaterialAction LightMaterialAction => lightMaterialAction;
    public AudioAction DumpAudioAction => dumpAudioAction;
    public AudioAction HitJimothyAudioAction => hitJimothyAudioAction;
    public AudioAction JimHonkVoiceAudioAction => jimHonkVoiceAudioAction;
    public AudioAction PickUpHazardAudioAction => pickUpHazardAudioAction;
    public ArmatureAttachment[] JimothyAttachments => jimothyAttachments;
    public ArmatureAttachment[] MachineAttachments => machineAttachments;

    public override string EnemyId => "Transporter";

    public override Skinner CreateSkinner()
    {
        return new JimothySkinner(this);
    }
}