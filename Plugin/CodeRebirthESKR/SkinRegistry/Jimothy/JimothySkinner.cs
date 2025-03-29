using System.Collections.Generic;
using AntlerShed.SkinRegistry;
using CodeRebirth.src.Content.Enemies;
using CodeRebirthESKR.Misc;
using UnityEngine;

namespace CodeRebirthESKR.SkinRegistry.Jimothy;
public class JimothySkinner(JimothySkin skinData) : Skinner, JimothyEventHandler
{

    protected VanillaMaterial vanillaSmallPartsMaterial;
    protected VanillaMaterial vanillaForkliftMaterial;
    protected VanillaMaterial vanillaLightMaterial;
    protected AudioClip vanillaDumpAudio;
    protected AudioClip vanillaHitJimothyAudio;
    protected AudioClip vanillaJimHonkVoiceAudio;
    protected AudioClip vanillaPickUpHazardAudio;
    protected List<GameObject> activeAttachementsForJimothy;
    protected List<GameObject> activeAttachmentsForMachine;

    protected JimothySkin SkinData { get; } = skinData;

    public void Apply(GameObject enemy)
    {
        Transporter transporter = enemy.GetComponent<Transporter>();

        activeAttachementsForJimothy = ArmatureAttachment.ApplyAttachments(SkinData.JimothyAttachments, transporter.skinnedMeshRenderers[0]);
        activeAttachmentsForMachine = ArmatureAttachment.ApplyAttachments(SkinData.MachineAttachments, transporter.skinnedMeshRenderers[1]);

        vanillaSmallPartsMaterial = SkinData.SmallPartsMaterialAction.Apply(transporter.skinnedMeshRenderers[0], 0);
        SkinData.SmallPartsMaterialAction.Apply(transporter.skinnedMeshRenderers[1], 1);

        vanillaForkliftMaterial = SkinData.ForkliftMaterialAction.Apply(transporter.skinnedMeshRenderers[1], 0);

        vanillaLightMaterial = SkinData.LightMaterialAction.Apply(transporter.skinnedMeshRenderers[1], 2);

        vanillaJimHonkVoiceAudio = SkinData.JimHonkVoiceAudioAction.Apply(ref transporter.jimHonkSound);
        vanillaDumpAudio = SkinData.DumpAudioAction.Apply(ref transporter.jimHonkSound);
        vanillaPickUpHazardAudio = SkinData.PickUpHazardAudioAction.Apply(ref transporter.pickUpHazardSound);
        vanillaHitJimothyAudio = SkinData.HitJimothyAudioAction.Apply(ref transporter.hitJimothySound);

        EnemySkinRegistry.RegisterEnemyEventHandler(transporter, this);

        //Perform any logic here to modify the appearance of the enemy. All of it must be client-side.
        //This is also the point where an EventHandler is registered if your skinner makes use of it. To do so, call EnemySkinRegistry.RegisterEventHandler(enemy, MyEventHandler)
    }

    public void Remove(GameObject enemy)
    {
        Transporter transporter = enemy.GetComponent<Transporter>();

        ArmatureAttachment.RemoveAttachments(activeAttachementsForJimothy);
        ArmatureAttachment.RemoveAttachments(activeAttachmentsForMachine);

        SkinData.SmallPartsMaterialAction.Remove(transporter.skinnedMeshRenderers[0], 0, vanillaSmallPartsMaterial);
        SkinData.SmallPartsMaterialAction.Remove(transporter.skinnedMeshRenderers[1], 1, vanillaSmallPartsMaterial);

        SkinData.ForkliftMaterialAction.Remove(transporter.skinnedMeshRenderers[1], 0, vanillaForkliftMaterial);

        SkinData.LightMaterialAction.Remove(transporter.skinnedMeshRenderers[1], 2, vanillaLightMaterial);

        SkinData.JimHonkVoiceAudioAction.Remove(ref transporter.jimHonkSound, vanillaJimHonkVoiceAudio);
        SkinData.DumpAudioAction.Remove(ref transporter.jimHonkSound, vanillaDumpAudio);
        SkinData.PickUpHazardAudioAction.Remove(ref transporter.pickUpHazardSound, vanillaPickUpHazardAudio);
        SkinData.HitJimothyAudioAction.Remove(ref transporter.hitJimothySound, vanillaHitJimothyAudio);

        EnemySkinRegistry.RemoveEnemyEventHandler(transporter, this);
        //Restore the enemy to its vanilla appearance, undoing all of the changes done by Apply.
        //Unregister the event handler by calling RemoveEventHandler(enemy) if you registered one.
    }
}