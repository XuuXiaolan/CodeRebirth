using System.Collections;
using CodeRebirth.src.MiscScripts;
using Dawn;
using Dusk;
using Dawn.Utils;
using GameNetcodeStuff;
using UnityEngine;

namespace CodeRebirth.src.Content.Maps;
public class BoomTrap : BearTrap
{
    public AudioSource boomSource = null!;
    public AudioClip explosionSound = null!;
    public AudioClip hissSound = null!;

    private bool triggeredOnce = false;

    public override void TriggerTrap(PlayerControllerB player)
    {
        base.TriggerTrap(player);
        if (triggeredOnce) return;
        triggeredOnce = true;
        StartCoroutine(StartExplosionCountdown(player));
    }

    private IEnumerator StartExplosionCountdown(PlayerControllerB? playerSnapped)
    {
        boomSource.PlayOneShot(hissSound);
        yield return new WaitForSeconds(hissSound.length);
        CRUtilities.CreateExplosion(transform.position, true, 400, 0f, 4f, 10, playerSnapped, null, 50f);
        boomSource.PlayOneShot(explosionSound);
        yield return new WaitForSeconds(explosionSound.length);
        if (playerCaught == null && playerSnapped != null && !playerSnapped.isPlayerDead && playerSnapped.IsLocalPlayer())
        {
            DuskModContent.Achievements.TryTriggerAchievement(CodeRebirthAchievementKeys.ShortFuse);
        }

        DoReleaseTrap();
        if (!IsServer) yield break;
        this.NetworkObject.Despawn();
    }

    public override void TriggerTrap(EnemyAI enemy)
    {
        base.TriggerTrap(enemy);
        if (triggeredOnce) return;
        triggeredOnce = true;
        StartExplosionCountdown(null);
    }
}