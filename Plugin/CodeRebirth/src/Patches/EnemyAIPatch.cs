using CodeRebirth.src.Content.Enemies;
using CodeRebirth.src.Content.Weapons;
using CodeRebirth.src.Util.Extensions;
using GameNetcodeStuff;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace CodeRebirth.src.Patches;
static class EnemyAIPatch
{
    private static Dictionary<EnemyAI, Coroutine> slowedEnemies = new Dictionary<EnemyAI, Coroutine>();
    private static System.Random enemyRandom = null;

    public static void Init()
    {
        On.EnemyAI.Start += EnemyAI_Start;
        On.EnemyAI.HitEnemy += EnemyAI_HitEnemy;
        On.EnemyAI.Update += EnemyAI_Update;
        On.EnemyAI.OnCollideWithPlayer += EnemyAI_OnCollideWithPlayer;
    }

    private static void EnemyAI_OnCollideWithPlayer(On.EnemyAI.orig_OnCollideWithPlayer orig, EnemyAI self, Collider other)
    {
        if (other.gameObject.layer == 19 && other.TryGetComponent(out PuppeteersVoodoo puppet))
        {
            if (self.isEnemyDead) return;
            if (!self.IsServer) return;
            if (puppet.lastTimeTakenDamageFromEnemy <= 1f) return;
            if (self is Puppeteer) return;
            if (self.enemyType.enemyName.Contains("SCP-999"))
            {
                puppet.Hit(-1, self.transform.position, null, false, -1);
                return;
            }
            puppet.Hit(1, self.transform.position, null, false, -1);
            return;
        }
        orig(self, other);
    }

    private static void EnemyAI_Start(On.EnemyAI.orig_Start orig, EnemyAI self)
    {
        orig(self);
        enemyRandom ??= new System.Random(StartOfRound.Instance.randomMapSeed);
        if (self is CaveDwellerAI)
        {
            self.gameObject.transform.Find("BabyMeshContainer").Find("BabyManeaterMesh").gameObject.layer = 19;
        }
    }

    private static void EnemyAI_Update(On.EnemyAI.orig_Update orig, EnemyAI self)
    {
        orig(self);
        if (slowedEnemies.ContainsKey(self) && slowedEnemies[self] != null)
        {
            self.agent.velocity = Vector3.zero;
        }
    }

    private static void EnemyAI_HitEnemy(On.EnemyAI.orig_HitEnemy orig, EnemyAI self, int force, PlayerControllerB playerWhoHit, bool playHitSFX, int hitID)
    {
        if (self != null && playerWhoHit != null && playerWhoHit.currentlyHeldObjectServer != null && playerWhoHit.currentlyHeldObjectServer.itemProperties != null && playerWhoHit.currentlyHeldObjectServer is IcyHammer)
        {
            if (enemyRandom.NextFloat(0, 100) <= 75f)
            {
                Plugin.ExtendedLogging("Slowed enemy");
                if (slowedEnemies.ContainsKey(self) && slowedEnemies[self] != null)
                {
                    self.StopCoroutine(slowedEnemies[self]);
                }
                slowedEnemies[self] = self.StartCoroutine(DelayResetSpeed(self));   
            }
        }

        if (self != null && playerWhoHit != null && playerWhoHit.currentlyHeldObjectServer != null && playerWhoHit.currentlyHeldObjectServer.itemProperties != null && playerWhoHit.currentlyHeldObjectServer is NaturesMace)
        {
            force = 0;
            self.enemyHP++;
            Plugin.ExtendedLogging($"Enemy HP: {self.enemyHP}");
        }

        orig(self, force, playerWhoHit, playHitSFX, hitID);
    }

    private static IEnumerator DelayResetSpeed(EnemyAI self)
    {
        yield return new WaitForSeconds(4f);
        slowedEnemies.Remove(self);
    }
}