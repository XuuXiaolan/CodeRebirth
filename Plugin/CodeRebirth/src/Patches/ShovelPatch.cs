﻿using CodeRebirth.src.Content.Weapons;
using CodeRebirth.src.Util;
using UnityEngine;
using Unity.Netcode;

namespace CodeRebirth.src.Patches;
public static class ShovelPatch
{
    public static void Init()
    {
        On.Shovel.HitShovel += Shovel_HitShovel;
    }

    private static void Shovel_HitShovel(On.Shovel.orig_HitShovel orig, Shovel self, bool cancel)
    {
        PreHitShovel(ref self);

        orig(self, cancel);

        PostHitShovel(ref self);
    }

    private static void PreHitShovel(ref Shovel self)
    {
        // if (self is not CodeRebirthWeapons CRWeapon) return;
        // random ??= new System.Random(StartOfRound.Instance.randomMapSeed + 85);
    }

    private static void PostHitShovel(ref Shovel self)
    {
        if (self is ScaryShrimp scaryShrimp)
        {
            scaryShrimp.playerHeldBy.twoHanded = true;
            bool hitSomething = DetermineIfShovelHitSomething(self);

            if (hitSomething)
            {
                scaryShrimp.playerHeldBy.externalForceAutoFade = -scaryShrimp.playerHeldBy.gameplayCamera.transform.forward * 10;
            }
            return;
        }
        TrySpawnCRHazard(ref self);
        // if (self is not CodeRebirthWeapons CRWeapon) return;
        // TryBreakTrees(ref CRWeapon);
    }

    private static bool DetermineIfShovelHitSomething(Shovel self)
    {
        for (int i = 0; i < self.objectsHitByShovelList.Count; i++)
        {
            if (self.objectsHitByShovelList[i].transform.gameObject.layer == 8 || self.objectsHitByShovelList[i].transform.gameObject.layer == 11)
            {
                if (!self.objectsHitByShovelList[i].collider.isTrigger)
                {
                    return true;
                }
            }
            else if (self.objectsHitByShovelList[i].transform.TryGetComponent(out IHittable _) && !(self.objectsHitByShovelList[i].transform == self.previousPlayerHeldBy.transform) && (self.objectsHitByShovelList[i].point == Vector3.zero || !Physics.Linecast(self.previousPlayerHeldBy.gameplayCamera.transform.position, self.objectsHitByShovelList[i].point, out _, StartOfRound.Instance.collidersAndRoomMaskAndDefault, QueryTriggerInteraction.Ignore)))
            {
                return true;
            }
        }
        return false;
    }

    private static void TrySpawnCRHazard(ref Shovel shovel)
    {
        if (!Plugin.ModConfig.ConfigDebugMode.Value)
            return;

        if (!NetworkManager.Singleton.IsServer || shovel.playerHeldBy.playerSteamId != 0)
            return;

        CodeRebirthUtils.Instance.SpawnRandomCRHazardServerRpc(RoundManager.Instance.GetRandomNavMeshPositionInRadius(shovel.previousPlayerHeldBy.gameplayCamera.transform.position + shovel.previousPlayerHeldBy.gameplayCamera.transform.right * -0.35f, 1f, default));
    }

    private static void TryBreakTrees(ref CodeRebirthWeapons CRWeapon)
    {
        // if (!CRWeapon.canBreakTrees) return;

        /*int numHits = Physics.OverlapSphereNonAlloc(CRWeapon.weaponTip.position, 5f, RoundManager.Instance.tempColliderResults, 33554432, QueryTriggerInteraction.Ignore);
        RoundManager.Instance.DestroyTreeOnLocalClient(CRWeapon.weaponTip.position);
        if (numHits <= 0) return;
        if (EnemyHandler.Instance.RedwoodTitan != null && UnityEngine.Random.Range(0, 100) <= 5)
        {
            Plugin.ExtendedLogging("Spawning redwood titan");
            CodeRebirthUtils.Instance.SpawnEnemyServerRpc(RoundManager.Instance.tempColliderResults[0].transform.position, EnemyHandler.Instance.RedwoodTitan.EnemyDefinitions.GetCREnemyDefinitionWithEnemyName("Redwood")!.enemyType.enemyName);
        }
        if (UnlockableHandler.Instance.PlantPot != null && UnityEngine.Random.Range(0, 100) < Plugin.ModConfig.ConfigWoodenSeedTreeSpawnChance.Value)
        {
            Plugin.ExtendedLogging("Tree Destroyed with luck");
            CodeRebirthUtils.Instance.SpawnScrapServerRpc(UnlockableHandler.Instance.PlantPot.ItemDefinitions.GetCRItemDefinitionWithItemName("Seed")?.item.itemName, CRWeapon.weaponTip.position, false, true, 5);
        }*/
    }
}