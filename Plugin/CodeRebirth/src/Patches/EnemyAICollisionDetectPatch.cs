using System.Collections;
using CodeRebirth.src.Content.Weapons;
using GameNetcodeStuff;
using UnityEngine;

namespace CodeRebirth.src.Patches;
public static class EnemyAICollisionDetectPatch
{
    public static void Init()
    {
        On.EnemyAICollisionDetect.IHittable_Hit += EnemyAICollisionDetect_IHittable_Hit;
    }

    private static bool EnemyAICollisionDetect_IHittable_Hit(On.EnemyAICollisionDetect.orig_IHittable_Hit orig, EnemyAICollisionDetect self, int force, Vector3 hitDirection, PlayerControllerB playerWhoHit, bool playHitSFX, int hitID)
    {
        if (playerWhoHit.currentlyHeldObjectServer != null && playerWhoHit.currentlyHeldObjectServer is ScaryShrimp scaryShrimp)
        {
            if (scaryShrimp.hitEnemy)
            {
                return false;
            }
            scaryShrimp.hitEnemy = true;
            self.mainScript.HitEnemyOnLocalClient(3, hitDirection, playerWhoHit, playHitSFX, hitID);
            if (self.mainScript.isEnemyDead || self.mainScript.enemyHP - 3 <= 0)
            {
                playerWhoHit.itemAudio.PlayOneShot(scaryShrimp.killClip); // network this.
            }
            self.StartCoroutine(DespawnHeldObject(playerWhoHit));
            return false;
        }
        return orig(self, force, hitDirection, playerWhoHit, playHitSFX, hitID);
    }

    private static IEnumerator DespawnHeldObject(PlayerControllerB playerWhoHit)
    {
        if (playerWhoHit.currentlyHeldObjectServer == null)
        {
            Plugin.ExtendedLogging("No held object");
            yield break;
        }
        GrabbableObject grabbableObject = playerWhoHit.currentlyHeldObjectServer;
        grabbableObject.originalScale = Vector3.zero;
        grabbableObject.transform.localScale = Vector3.zero;
        yield return new WaitForSeconds(0.1f);
        Plugin.ExtendedLogging("Despawned held object");
        playerWhoHit.DiscardHeldObject();
    }
}