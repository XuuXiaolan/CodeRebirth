using System.Collections;
using System.Collections.Generic;
using System.Linq;
using CodeRebirth.src.MiscScripts;
using GameNetcodeStuff;
using UnityEngine;

namespace CodeRebirth.src.Content.Enemies;
public class CutieFlyAI : CodeRebirthEnemyAI
{
    public Material[] variantMaterials = [];

    private System.Random cutieflyRandom = new();
    private static readonly int IsDeadAnimation = Animator.StringToHash("doDeath");
    private static List<CutieFlyAI> cutieflys = new();
    private float oldSpeed = 0f;

    public override void Start()
    {
        base.Start();
        oldSpeed = agent.speed;
        cutieflys.Add(this);

        // Random seed for variant material
        cutieflyRandom = new System.Random(StartOfRound.Instance.randomMapSeed + cutieflys.Count);

        // Apply material variant
        ApplyMaterialVariant();
        if (IsServer) smartAgentNavigator.StartSearchRoutine(transform.position, 50);
    }

    private void ApplyMaterialVariant()
    {
        Material variantMaterial = variantMaterials[cutieflyRandom.Next(variantMaterials.Length)];
        Material[] currentMaterials = skinnedMeshRenderers[0].sharedMaterials;
        currentMaterials[0] = variantMaterial;
        skinnedMeshRenderers[0].SetMaterials(currentMaterials.ToList());
    }

    public override void OnCollideWithPlayer(Collider other)
    {
        base.OnCollideWithPlayer(other);
        if (isEnemyDead) return;
        HitEnemy(1, other.GetComponent<PlayerControllerB>(), true, -1);
    }

    public override void HitEnemy(int force = 1, PlayerControllerB? playerWhoHit = null, bool playHitSFX = false, int hitID = -1)
    {
        base.HitEnemy(force, playerWhoHit, playHitSFX, hitID);
        if (isEnemyDead || playerWhoHit == null) return;
        enemyHP -= force;
        if (IsOwner && enemyHP <= 0)
        {
            KillEnemyOnOwnerClient();
        }
    }

    public override void KillEnemy(bool destroy = false)
    {
        base.KillEnemy(destroy);
        smartAgentNavigator.StopSearchRoutine();
        if (IsServer) creatureNetworkAnimator.SetTrigger(IsDeadAnimation);
    }

    public void LandCutieflyAnimEvent()
    {
        agent.velocity = Vector3.zero;
        agent.speed = 0;
    }

    public void FlyCutieflyAnimEvent()
    {
        agent.speed = oldSpeed;
    }

    public void SpawnMonarchAnimEvent()
    {
        if (!IsServer) return;
        if (Monarch.Monarchs.Count > 0)
        {
            if (UnityEngine.Random.Range(0, 100) < 50) return;
        }
        RoundManager.Instance.SpawnEnemyGameObject(transform.position, -1, -1, EnemyHandler.Instance.Monarch.MonarchEnemyType);
    }
}