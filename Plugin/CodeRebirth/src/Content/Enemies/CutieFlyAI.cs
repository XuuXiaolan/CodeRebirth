using System.Collections;
using Dawn;
using GameNetcodeStuff;
using UnityEngine;

namespace CodeRebirth.src.Content.Enemies;
public class CutieFlyAI : CodeRebirthEnemyAI
{
    public GameObject armature = null!;

    private static readonly int IsDeadAnimation = Animator.StringToHash("doDeath"); // Trigger
    private static readonly int IsLeavingAnimation = Animator.StringToHash("leaving"); // Bool
    private float oldSpeed = 0f;
    private float timeLeft = 30;

    public override void Start()
    {
        base.Start();
        oldSpeed = agent.speed;

        if (IsServer) smartAgentNavigator.StartSearchRoutine(50);
    }

    public override void Update()
    {
        base.Update();
        if (!IsServer)
        {
            return;
        }

        if (daytimeEnemyLeaving)
        {
            timeLeft -= Time.deltaTime;
            armature.gameObject.transform.position += Vector3.up * Time.deltaTime * 2f;
            if (timeLeft <= 0)
            {
                NetworkObject.Despawn(true);
            }
        }
    }

    public override void DaytimeEnemyLeave()
    {
        base.DaytimeEnemyLeave();
        if (IsServer)
        {
            creatureAnimator.SetBool(IsLeavingAnimation, true);
        }
    }

    public override void OnCollideWithPlayer(Collider other)
    {
        base.OnCollideWithPlayer(other);
        if (isEnemyDead)
        {
            return;
        }

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
        if (IsServer && creatureNetworkAnimator != null) creatureNetworkAnimator.SetTrigger(IsDeadAnimation);
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

            var enemyType = LethalContent.Enemies[CodeRebirthEnemyKeys.Monarch].EnemyType;
            RoundManager.Instance.SpawnEnemyGameObject(transform.position, -1, -1, enemyType);
        }
        HUDManager.Instance.DisplayTip("WARNING", "SEISMIC ACTIVITY DETECTED", true);
        HUDManager.Instance.ShakeCamera(ScreenShakeType.VeryStrong);
        HUDManager.Instance.ShakeCamera(ScreenShakeType.Long);
    }
}