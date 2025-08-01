using System.Collections;
using CodeRebirthLib.ContentManagement.Enemies;
using GameNetcodeStuff;
using UnityEngine;

namespace CodeRebirth.src.Content.Enemies;
public class CutieFlyAI : CodeRebirthEnemyAI
{
    private static readonly int IsDeadAnimation = Animator.StringToHash("doDeath");
    private float oldSpeed = 0f;

    public override void Start()
    {
        base.Start();
        oldSpeed = agent.speed;

        if (IsServer) smartAgentNavigator.StartSearchRoutine(50);
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
        if (IsServer && creatureNetworkAnimator !=  null) creatureNetworkAnimator.SetTrigger(IsDeadAnimation);
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
        StartCoroutine(HandleSpawningMonarch());
    }

    public IEnumerator HandleSpawningMonarch()
    {
        if (EnemyHandler.Instance.Monarch == null) yield break;
        yield return new WaitForSeconds(3f);
        if (IsServer)
        {
            if (Monarch.Monarchs.Count > 0)
            {
                if (UnityEngine.Random.Range(0, 100) < 50) yield break;
            }
            if (Plugin.Mod.EnemyRegistry().TryGetFromEnemyName("Monarch", out CREnemyDefinition? CREnemyDefinition))
            {
                RoundManager.Instance.SpawnEnemyGameObject(transform.position, -1, -1, CREnemyDefinition.EnemyType);
            }
        }
        HUDManager.Instance.DisplayTip("WARNING", "SEISMIC ACTIVITY DETECTED", true);
        HUDManager.Instance.ShakeCamera(ScreenShakeType.VeryStrong);
        HUDManager.Instance.ShakeCamera(ScreenShakeType.Long);
    }
}