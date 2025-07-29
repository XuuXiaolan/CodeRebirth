using CodeRebirth.src.Content.Enemies;
using CodeRebirth.src.Content.Items;
using CodeRebirth.src.Util;
using CodeRebirth.src.Util.Extensions;
using CodeRebirthLib.ContentManagement.Achievements;
using CodeRebirthLib.ContentManagement.Enemies;
using CodeRebirthLib.ContentManagement.MapObjects;
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
    }

    private static void EnemyAI_KillEnemy(On.EnemyAI.orig_KillEnemy orig, EnemyAI self, bool destroy)
    {
        orig(self, destroy);

        CREnemyAdditionalData additionalEnemyData = CREnemyAdditionalData.CreateOrGet(self);
        if (!self.isEnemyDead)
            return;

        if (!additionalEnemyData.KilledByPlayer)
            return;

        if (additionalEnemyData.PlayerThatLastHit!.IsLocalPlayer())
        {
            if (self is RedwoodTitanAI && Plugin.Mod.AchievementRegistry().TryGetFromAchievementName("Timber!", out CRAchievementBaseDefinition? TimberAchievementDefinition))
            {
                ((CRInstantAchievement)TimberAchievementDefinition).TriggerAchievement();
            }
            else if (self is DriftwoodMenaceAI && Plugin.Mod.AchievementRegistry().TryGetFromAchievementName("Bushwacked", out CRAchievementBaseDefinition? BushwackedAchievmentDefinition))
            {
                ((CRInstantAchievement)BushwackedAchievmentDefinition).TriggerAchievement();
            }
        }

        if (CodeRebirthUtils.Instance.enemyCoinDropRate.TryGetValue(self.enemyType, out float coinDropChance))
        {
            float coinChance = coinDropChance;
            Plugin.ExtendedLogging($"Rolling to drop coin {coinChance}");

            if (!NetworkManager.Singleton.IsServer)
                return;

            if (UnityEngine.Random.Range(0f, 100f) >= coinChance)
                return;

            if (!Plugin.Mod.MapObjectRegistry().TryGetFromMapObjectName("Money", out CRMapObjectDefinition? moneyMapObjectDefinition))
                return;

            GameObject coin = UnityEngine.Object.Instantiate(moneyMapObjectDefinition.GameObject, self.transform.position, Quaternion.identity, RoundManager.Instance.mapPropsContainer.transform);
            coin.GetComponent<NetworkObject>().Spawn(true);
        }
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

        if (RoundManager.Instance.currentLevel.sceneName != "Oxyde" || StartOfRound.Instance.inShipPhase)
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
        if (_slowedEnemies.TryGetValue(self, out _))
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