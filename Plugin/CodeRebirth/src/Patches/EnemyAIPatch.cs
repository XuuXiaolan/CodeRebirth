using CodeRebirth.src.Content.Enemies;
using CodeRebirth.src.Content.Items;
using CodeRebirth.src.Util;
using Dawn;
using Dawn.Utils;
using GameNetcodeStuff;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Unity.Netcode;
using UnityEngine;

namespace CodeRebirth.src.Patches;

static class EnemyAIPatch
{
    private static Dictionary<EnemyAI, Coroutine> _slowedEnemies = new();

    public static void Init()
    {
        On.EnemyAI.Start += EnemyAI_Start;
        On.EnemyAI.HitEnemy += EnemyAI_HitEnemy;
        On.EnemyAI.KillEnemy += EnemyAI_KillEnemy;
        On.EnemyAI.Update += EnemyAI_Update;
        On.EnemyAI.OnCollideWithPlayer += EnemyAI_OnCollideWithPlayer;

        LethalContent.Enemies.OnFreezeWithContext += (_) => FixManeaterForSeamine();
    }

    private static void FixManeaterForSeamine()
    {
        LethalContent.Enemies[EnemyKeys.Maneater].EnemyType.enemyPrefab.transform.Find("BabyMeshContainer").Find("BabyManeaterMesh").gameObject.layer = 19;
    }

    private static void EnemyAI_KillEnemy(On.EnemyAI.orig_KillEnemy orig, EnemyAI self, bool destroy)
    {
        orig(self, destroy);

        if (!NetworkManager.Singleton.IsServer)
            return;

        if (!self.isEnemyDead)
            return;

        if (!LethalContent.MapObjects.TryGetValue(CodeRebirthMapObjectKeys.CrispDollarBill, out DawnMapObjectInfo mapObjectInfo))
            return;

        DawnEnemyAdditionalData additionalEnemyData = DawnEnemyAdditionalData.CreateOrGet(self);
        if (!additionalEnemyData.KilledByPlayer)
            return;

        if (CodeRebirthUtils.Instance.enemyDollarBillDropRate.TryGetValue(self.enemyType, out float dollarBillDropChance))
        {
            float dollarBillChance = dollarBillDropChance;
            Plugin.ExtendedLogging($"Rolling to drop Dollar Bill {dollarBillChance}");

            if (UnityEngine.Random.Range(0f, 100f) >= dollarBillChance)
                return;

            GameObject dollarBill = UnityEngine.Object.Instantiate(mapObjectInfo.GetMapObjectPrefab()!, self.transform.position, Quaternion.identity, RoundManager.Instance.mapPropsContainer.transform);
            dollarBill.GetComponent<NetworkObject>().Spawn(true);
        }
    }

    private static void EnemyAI_OnCollideWithPlayer(On.EnemyAI.orig_OnCollideWithPlayer orig, EnemyAI self, Collider other)
    {
        if (other.gameObject.layer != 19 || self.isEnemyDead || !self.IsServer || self is Puppeteer || !other.TryGetComponent(out PuppeteersVoodoo puppet))
        {
            orig(self, other);
            return;
        }

        if (puppet.lastTimeTakenDamageFromEnemy <= 1f)
        {
            return;
        }

        foreach (EnemyType enemyBlacklisted in PuppeteersVoodoo.blacklistedEnemies)
        {
            if (self.enemyType == enemyBlacklisted)
            {
                return;
            }
        }

        if (self.enemyType.enemyName.Contains("SCP-999"))
        {
            puppet.Hit(-1, self.transform.position, null, false, -1);
            return;
        }
        puppet.Hit(1, self.transform.position, null, false, -1);
    }

    private static void EnemyAI_Start(On.EnemyAI.orig_Start orig, EnemyAI self)
    {
        orig(self);
        if (StartOfRound.Instance.inShipPhase || !RoundManager.Instance.currentLevel.sceneName.Equals("Oxyde"))
            return;

        self.SetEnemyOutside(true);
        GameObject? favouriteSpot = RoundManager.Instance.outsideAINodes.OrderBy(x => Vector3.Distance(x.transform.position, self.transform.position)).FirstOrDefault();
        if (favouriteSpot == null)
            return;

        self.favoriteSpot = favouriteSpot.transform;
    }

    private static void EnemyAI_Update(On.EnemyAI.orig_Update orig, EnemyAI self)
    {
        orig(self);
        if (_slowedEnemies.ContainsKey(self))
        {
            self.agent.velocity = Vector3.zero;
        }
    }

    private static void EnemyAI_HitEnemy(On.EnemyAI.orig_HitEnemy orig, EnemyAI self, int force, PlayerControllerB? playerWhoHit, bool playHitSFX, int hitID)
    {
        if (playerWhoHit != null && playerWhoHit.currentlyHeldObjectServer != null && playerWhoHit.currentlyHeldObjectServer.itemProperties != null && playerWhoHit.currentlyHeldObjectServer.itemProperties.isDefensiveWeapon && playerWhoHit.currentlyHeldObjectServer is Mountaineer mountaineer)
        {
            if (CodeRebirthUtils.Instance.CRRandom.NextFloat(0, 100) <= mountaineer.FreezePercentile)
            {
                Plugin.ExtendedLogging("Slowed enemy");
                if (_slowedEnemies.ContainsKey(self) && _slowedEnemies[self] != null)
                {
                    self.StopCoroutine(_slowedEnemies[self]);
                }
                _slowedEnemies[self] = self.StartCoroutine(DelayResetSpeed(self));
            }
        }
        orig(self, force, playerWhoHit, playHitSFX, hitID);
    }

    private static IEnumerator DelayResetSpeed(EnemyAI self)
    {
        yield return new WaitForSeconds(4f);
        _slowedEnemies.Remove(self);
    }
}