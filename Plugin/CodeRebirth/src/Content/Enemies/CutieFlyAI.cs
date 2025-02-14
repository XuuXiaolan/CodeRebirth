using System.Collections.Generic;
using System.Linq;
using GameNetcodeStuff;
using UnityEngine;

namespace CodeRebirth.src.Content.Enemies;
public class CutieFlyAI : CodeRebirthEnemyAI
{
    public Material[] variantMaterials = [];

    private System.Random cutieflyRandom = new();
    private static readonly int IsDeadAnimation = Animator.StringToHash("doDeath");
    private static List<CutieFlyAI> cutieflys = new();

    public override void Start()
    {
        base.Start();
        cutieflys.Add(this);

        // Random seed for variant material
        cutieflyRandom = new System.Random(StartOfRound.Instance.randomMapSeed + cutieflys.Count);

        // Apply material variant
        ApplyMaterialVariant();
        smartAgentNavigator.StartSearchRoutine(transform.position, 50);
    }

    private void ApplyMaterialVariant()
    {
        Material variantMaterial = variantMaterials[cutieflyRandom.Next(variantMaterials.Length)];
        Material[] currentMaterials = skinnedMeshRenderers[0].sharedMaterials;
        currentMaterials[0] = variantMaterial;
        skinnedMeshRenderers[0].SetMaterials(currentMaterials.ToList());
    }

    public override void HitEnemy(int force = 1, PlayerControllerB?playerWhoHit = null, bool playHitSFX = false, int hitID = -1)
    {
        base.HitEnemy(force, playerWhoHit, playHitSFX, hitID);
        if (isEnemyDead) return;
        enemyHP -= force;
        if (IsOwner && enemyHP <= 0)
        {
            KillEnemyOnOwnerClient();
        }
    }

    public override void KillEnemy(bool destroy = false)
    {
        base.KillEnemy(destroy);
        if (IsServer) creatureNetworkAnimator.SetTrigger(IsDeadAnimation);
    }

    public void SpawnMonarchAnimEvent()
    {
        if (!IsServer) return;
        RoundManager.Instance.SpawnEnemyGameObject(transform.position, -1, -1, EnemyHandler.Instance.Monarch.MonarchEnemyType);
    }
}