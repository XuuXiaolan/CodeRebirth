using System;
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
        if (playerWhoHit != null && playerWhoHit.currentlyHeldObjectServer != null && playerWhoHit.currentlyHeldObjectServer is ScaryShrimp scaryShrimp)
        {
            if (scaryShrimp.hitEnemy.Value)
            {
                return false;
            }
            scaryShrimp.hitEnemy.Value = true;
            scaryShrimp.PastHitEnemyServerRpc(self.mainScript.enemyHP);
            self.mainScript.HitEnemyOnLocalClient(3, hitDirection, playerWhoHit, playHitSFX, hitID);
            return false;
        }
        return orig(self, force, hitDirection, playerWhoHit, playHitSFX, hitID);
    }
}