using GameNetcodeStuff;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace CodeRebirth.Patches;
static class EnemyAIPatch
{
    private static Dictionary<EnemyAI, Coroutine> slowedEnemies = new Dictionary<EnemyAI, Coroutine>();

    public static void Init()
    {
        On.EnemyAI.HitEnemy += EnemyAI_HitEnemy;
    }

    private static void EnemyAI_HitEnemy(On.EnemyAI.orig_HitEnemy orig, EnemyAI self, int force, PlayerControllerB playerWhoHit, bool playHitSFX, int hitID)
    {
        var oldAcceleration = self.agent.acceleration;
        if (self != null && playerWhoHit != null && playerWhoHit.currentlyHeldObjectServer != null && playerWhoHit.currentlyHeldObjectServer.itemProperties != null && playerWhoHit.currentlyHeldObjectServer.itemProperties.itemName == "Icy Hammer")
        {
            if (!slowedEnemies.ContainsKey(self))
            {
                self.agent.acceleration *= 0f;
            }
            
            if (slowedEnemies.ContainsKey(self) && slowedEnemies[self] != null)
            {
                self.StopCoroutine(slowedEnemies[self]);
            }

            slowedEnemies[self] = self.StartCoroutine(DelayResetAcceleration(self));
        }

        if (self != null && playerWhoHit != null && playerWhoHit.currentlyHeldObjectServer != null && playerWhoHit.currentlyHeldObjectServer.itemProperties != null && playerWhoHit.currentlyHeldObjectServer.itemProperties.itemName == "Nature's Mace") {
            force = 0;
            self.enemyHP++;
            Plugin.ExtendedLogging($"Enemy HP: {self.enemyHP}");
        }
        
        orig(self, force, playerWhoHit, playHitSFX, hitID);
    }

    private static IEnumerator DelayResetAcceleration(EnemyAI self)
    {
        yield return new WaitForSeconds(5f);
        self.agent.acceleration /= 0.2f;  // Resetting the acceleration to the original value
        slowedEnemies.Remove(self);
    }
}