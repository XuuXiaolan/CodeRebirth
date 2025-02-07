using System.Collections;
using CodeRebirth.src.MiscScripts;
using GameNetcodeStuff;
using UnityEngine;

namespace CodeRebirth.src.Content.Maps;
public class BoomTrap : BearTrap
{
    public AudioClip hissSound = null!;
    public bool triggeredOnce = false;

    public override void TriggerTrap(PlayerControllerB player)
    {
        base.TriggerTrap(player);
        if (triggeredOnce) return;
        triggeredOnce = true;
        StartCoroutine(StartExplosionCountdown(player));
    }

    private IEnumerator StartExplosionCountdown(PlayerControllerB? playerSnapped)
    {
        trapAudioSource.PlayOneShot(hissSound);
        yield return new WaitForSeconds(hissSound.length);
        yield return new WaitForSeconds(2f);
        CRUtilities.CreateExplosion(transform.position, true, 200, 0, 1f, 10, playerSnapped, null, 50f);
        yield return new WaitForSeconds(0.4f);
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