using CodeRebirth.src.Content.Weapons;
using CodeRebirth.src.Util.Extensions;
using GameNetcodeStuff;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace CodeRebirth.src.Patches;
static class EnemyAIPatch
{
    private static Dictionary<EnemyAI, float> speedOfEnemies = new Dictionary<EnemyAI, float>();
    private static Dictionary<EnemyAI, Coroutine> slowedEnemies = new Dictionary<EnemyAI, Coroutine>();
    private static System.Random enemyRandom;

    public static void Init()
    {
        On.EnemyAI.Start += EnemyAI_Start;
        On.EnemyAI.HitEnemy += EnemyAI_HitEnemy;
        On.EnemyAI.Update += EnemyAI_Update;
    }

    private static void EnemyAI_Start(On.EnemyAI.orig_Start orig, EnemyAI self)
    {
        orig(self);
        if (enemyRandom == null) enemyRandom = new System.Random(StartOfRound.Instance.randomMapSeed);
    }

    private static void EnemyAI_Update(On.EnemyAI.orig_Update orig, EnemyAI self)
    {
        if (slowedEnemies.ContainsKey(self) && slowedEnemies[self] != null)
        {
            self.agent.speed = 0f;
        }
        orig(self);
    }

    private static void EnemyAI_HitEnemy(On.EnemyAI.orig_HitEnemy orig, EnemyAI self, int force, PlayerControllerB playerWhoHit, bool playHitSFX, int hitID)
    {
        if (self != null && playerWhoHit != null && playerWhoHit.currentlyHeldObjectServer != null && playerWhoHit.currentlyHeldObjectServer.itemProperties != null && playerWhoHit.currentlyHeldObjectServer is IcyHammer)
        {   
            if (enemyRandom.NextFloat(0, 100) <= 25) {
                Plugin.ExtendedLogging("Slowed enemy");
                if (!speedOfEnemies.ContainsKey(self)) speedOfEnemies[self] = self.agent.speed;
                if (slowedEnemies.ContainsKey(self) && slowedEnemies[self] != null)
                {
                    self.StopCoroutine(slowedEnemies[self]);
                }
                slowedEnemies[self] = self.StartCoroutine(DelayResetSpeed(self));   
            }
        }

        if (self != null && playerWhoHit != null && playerWhoHit.currentlyHeldObjectServer != null && playerWhoHit.currentlyHeldObjectServer.itemProperties != null && playerWhoHit.currentlyHeldObjectServer is NaturesMace) {
            force = 0;
            self.enemyHP++;
            Plugin.ExtendedLogging($"Enemy HP: {self.enemyHP}");
        }
        
        orig(self, force, playerWhoHit, playHitSFX, hitID);
    }

    private static IEnumerator DelayResetSpeed(EnemyAI self)
    {
        yield return new WaitForSeconds(2.5f);
        slowedEnemies.Remove(self);
        self.agent.speed = speedOfEnemies[self];
        speedOfEnemies.Remove(self);
    }
}