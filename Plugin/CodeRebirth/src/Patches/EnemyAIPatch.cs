using CodeRebirth.src.Content.Enemies;
using CodeRebirth.src.Content.Items;
using CodeRebirth.src.Content.Maps;
using CodeRebirth.src.MiscScripts;
using CodeRebirth.src.Util;
using CodeRebirth.src.Util.Extensions;
using GameNetcodeStuff;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Unity.Netcode;
using UnityEngine;

namespace CodeRebirth.src.Patches;
static class EnemyAIPatch
{
    private static Dictionary<EnemyAI, Coroutine> slowedEnemies = new();

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
        if (self is CaveDwellerAI)
        {
            self.gameObject.transform.Find("BabyMeshContainer").Find("BabyManeaterMesh").gameObject.layer = 19;
        }

        if (RoundManager.Instance.currentLevel.sceneName != "Oxyde" || StartOfRound.Instance.inShipPhase) return;
        self.SetEnemyOutside(true);
        GameObject? favouriteSpot = RoundManager.Instance.outsideAINodes.OrderBy(x => Vector3.Distance(x.transform.position, self.transform.position)).FirstOrDefault();
        if (favouriteSpot == null)
            return;

        self.favoriteSpot = favouriteSpot.transform;
    }

    private static void EnemyAI_Update(On.EnemyAI.orig_Update orig, EnemyAI self)
    {
        orig(self);
        if (slowedEnemies.TryGetValue(self, out _))
        {
            self.agent.velocity = Vector3.zero;
        }
    }

    private static void EnemyAI_HitEnemy(On.EnemyAI.orig_HitEnemy orig, EnemyAI self, int force, PlayerControllerB? playerWhoHit, bool playHitSFX, int hitID)
    {
        if (playerWhoHit != null && playerWhoHit.currentlyHeldObjectServer != null && playerWhoHit.currentlyHeldObjectServer.itemProperties != null && playerWhoHit.currentlyHeldObjectServer is Mountaineer mountaineer)
        {
            if (CodeRebirthUtils.Instance.CRRandom.NextFloat(0, 100) <= mountaineer.FreezePercentile)
            {
                Plugin.ExtendedLogging("Slowed enemy");
                if (slowedEnemies.ContainsKey(self) && slowedEnemies[self] != null)
                {
                    self.StopCoroutine(slowedEnemies[self]);
                }
                slowedEnemies[self] = self.StartCoroutine(DelayResetSpeed(self));
            }
        }

        bool alreadyDead = false;
        if (self.isEnemyDead || self.enemyHP <= 0)
        {
            alreadyDead = true;
        }
        orig(self, force, playerWhoHit, playHitSFX, hitID);

        if (alreadyDead)
            return;

        if (self.IsServer && self.enemyHP - force <= 0 && playerWhoHit != null)
        {
            if (!CodeRebirthUtils.Instance.enemyCoinDropRate.ContainsKey(self.enemyType))
            {
                CodeRebirthUtils.Instance.enemyCoinDropRate.Add(self.enemyType, 0);
            }
            float coinChance = CodeRebirthUtils.Instance.enemyCoinDropRate[self.enemyType];
            Plugin.ExtendedLogging($"Rolling to drop coin {coinChance}");
            if (UnityEngine.Random.Range(0f, 100f) >= coinChance)
                return;

            GameObject? coinGO = MapObjectHandler.Instance.GetPrefabFor(SpawnSyncedCRObject.CRObjectType.Coin);
            if (coinGO == null)
                return;

            GameObject coin = UnityEngine.Object.Instantiate(coinGO, self.transform.position, Quaternion.identity, RoundManager.Instance.mapPropsContainer.transform);
            coin.GetComponent<NetworkObject>().Spawn(true);
        }
    }

    private static IEnumerator DelayResetSpeed(EnemyAI self)
    {
        yield return new WaitForSeconds(4f);
        slowedEnemies.Remove(self);
    }
}