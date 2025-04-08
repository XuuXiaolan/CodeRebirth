using System.Collections.Generic;
using System.Linq;
using AntlerShed.SkinRegistry;
using CodeRebirthESKR.Misc;
using UnityEngine;

namespace CodeRebirthESKR.SkinRegistry.Generic;
public class GenericSkinner(GenericSkin skinData) : Skinner, GenericEventHandler
{
    protected List<VanillaMaterial> allMaterials;
    protected VanillaMaterial vanillaMapDotMaterial;
    protected AudioClip vanillaDeathAudio;
    protected AudioClip vanillaHitBodyAudio;
    protected List<GameObject> activeAttachmentsForGeneric;

    protected GenericSkin SkinData { get; } = skinData;

    public void Apply(GameObject enemy)
    {
        EnemyAI enemyScript = enemy.GetComponent<EnemyAI>();

        if (SkinData.EnsureRenderersAreSet)
        {
            enemyScript.skinnedMeshRenderers = enemyScript.gameObject.GetComponentsInChildren<SkinnedMeshRenderer>();
        }

        foreach (var renderer in enemyScript.skinnedMeshRenderers)
        {
            activeAttachmentsForGeneric = activeAttachmentsForGeneric.Concat(ArmatureAttachment.ApplyAttachments(SkinData.GenericAttachments, renderer)).ToList();
        }

        foreach (var materialAction in SkinData.AllMaterialsAction)
        {
            allMaterials.Add(materialAction.Apply(enemyScript.skinnedMeshRenderers[materialAction.rendererIndex], materialAction.materialRendererIndex));
        }

        MeshRenderer? mapDotMeshRenderer = enemyScript.gameObject.transform.GetComponentsInChildren<MeshRenderer>().Where(x => x.CompareTag("DoNotSet") && x.gameObject.layer == 14).FirstOrDefault();
        if (mapDotMeshRenderer != null)
        {
            vanillaMapDotMaterial = SkinData.MapDotMaterialAction.Apply(mapDotMeshRenderer, 0);
        }
        vanillaHitBodyAudio = SkinData.HitBodyAudioAction.Apply(ref enemyScript.enemyType.hitBodySFX);
        vanillaDeathAudio = SkinData.DeathAudioAction.Apply(ref enemyScript.dieSFX);

        EnemySkinRegistry.RegisterEnemyEventHandler(enemyScript, this);

        //Perform any logic here to modify the appearance of the enemy. All of it must be client-side.
        //This is also the point where an EventHandler is registered if your skinner makes use of it. To do so, call EnemySkinRegistry.RegisterEventHandler(enemy, MyEventHandler)
    }

    public void Remove(GameObject enemy)
    {
        EnemyAI enemyScript = enemy.GetComponent<EnemyAI>();

        ArmatureAttachment.RemoveAttachments(activeAttachmentsForGeneric);

        for (int i = 0; i < allMaterials.Count; i++)
        {
            MaterialAction materialAction = SkinData.AllMaterialsAction[i];
            materialAction.Remove(enemyScript.skinnedMeshRenderers[materialAction.rendererIndex], materialAction.materialRendererIndex, allMaterials[i]);
        }

        MeshRenderer? mapDotMeshRenderer = enemyScript.gameObject.transform.GetComponentsInChildren<MeshRenderer>().Where(x => x.CompareTag("DoNotSet") && x.gameObject.layer == 14).FirstOrDefault();
        if (mapDotMeshRenderer != null)
        {
            SkinData.MapDotMaterialAction.Remove(mapDotMeshRenderer, 0, vanillaMapDotMaterial);
        }

        SkinData.HitBodyAudioAction.Remove(ref enemyScript.enemyType.hitBodySFX, vanillaHitBodyAudio);
        SkinData.DeathAudioAction.Remove(ref enemyScript.dieSFX, vanillaDeathAudio);

        EnemySkinRegistry.RemoveEnemyEventHandler(enemyScript, this);
        //Restore the enemy to its vanilla appearance, undoing all of the changes done by Apply.
        //Unregister the event handler by calling RemoveEventHandler(enemy) if you registered one.
    }
}