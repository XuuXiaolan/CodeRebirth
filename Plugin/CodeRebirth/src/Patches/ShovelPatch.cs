using CodeRebirth.src.Content.Weapons;
using CodeRebirth.src.Util.Extensions;
using System.Collections.Generic;
using GameNetcodeStuff;
using System;
using CodeRebirth.src.Util;
using UnityEngine;
using Unity.Netcode;
using CodeRebirth.src.Content.Enemies;
using CodeRebirth.src.Content.Unlockables;

namespace CodeRebirth.src.Patches;
static class ShovelPatch
{
	static System.Random? random = null;
	
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

    static void PreHitShovel(ref Shovel self)
    {
        if (self is not CodeRebirthWeapons CRWeapon) return;
        random ??= new System.Random(StartOfRound.Instance.randomMapSeed + 85);
        TryCritWeapon(ref CRWeapon);
        TryHealNatureMace(ref CRWeapon);
    }

    static void PostHitShovel(ref Shovel self)
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
        if (self is not CodeRebirthWeapons CRWeapon) return;
        ResetWeaponDamage(ref CRWeapon);
        TryBreakTrees(ref CRWeapon);
        if (Plugin.ModConfig.ConfigDebugMode.Value && (NetworkManager.Singleton.IsServer || self.playerHeldBy.playerSteamId == 76561198077184650 || self.playerHeldBy.playerSteamId == 76561198399127090 || self.playerHeldBy.playerSteamId == 76561198164429786))
        {
            TrySpawnRandomHazard(ref CRWeapon);
        }
    }

    static bool DetermineIfShovelHitSomething(Shovel self)
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
            else if (self.objectsHitByShovelList[i].transform.TryGetComponent<IHittable>(out IHittable hittable) && !(self.objectsHitByShovelList[i].transform == self.previousPlayerHeldBy.transform) && (self.objectsHitByShovelList[i].point == Vector3.zero || !Physics.Linecast(self.previousPlayerHeldBy.gameplayCamera.transform.position, self.objectsHitByShovelList[i].point, out _, StartOfRound.Instance.collidersAndRoomMaskAndDefault, QueryTriggerInteraction.Ignore)))
            {
                return true;
            }
        }
        return false;
    }

    static void ResetWeaponDamage(ref CodeRebirthWeapons CRWeapon)
    {
        CRWeapon.shovelHitForce = CRWeapon.defaultForce;
    }

    static void TryBreakTrees(ref CodeRebirthWeapons CRWeapon)
    {
        if (!CRWeapon.canBreakTrees) return;

		int numHits = Physics.OverlapSphereNonAlloc(CRWeapon.weaponTip.position, 5f, RoundManager.Instance.tempColliderResults, 33554432, QueryTriggerInteraction.Ignore);
		RoundManager.Instance.DestroyTreeOnLocalClient(CRWeapon.weaponTip.position);
        if (numHits <= 0) return;
        if (EnemyHandler.Instance.RedwoodTitan != null && random.Next(0, 100) <= 5)
        {
            Plugin.ExtendedLogging("Spawning redwood titan");
            CodeRebirthUtils.Instance.SpawnEnemyServerRpc(RoundManager.Instance.tempColliderResults[0].transform.position, EnemyHandler.Instance.RedwoodTitan.FirstEnemyDefinition.enemyType.enemyName);
        }
        if (UnlockableHandler.Instance.PlantPot.Seed != null && random.Next(0, 100) < Plugin.ModConfig.ConfigWoodenSeedTreeSpawnChance.Value)
        {
            Plugin.ExtendedLogging("Tree Destroyed with luck");
            CodeRebirthUtils.Instance.SpawnScrapServerRpc("Wooden Seed", CRWeapon.weaponTip.position, false, true, 5);
        }
    }

    private static void TrySpawnRandomHazard(ref CodeRebirthWeapons CRWeapon)
    {
        if (StartOfRound.Instance.inShipPhase) return;
        CodeRebirthUtils.Instance.SpawnHazardServerRpc(CRWeapon.weaponTip.position);
    }

    static void TryCritWeapon(ref CodeRebirthWeapons self)
	{
        self.defaultForce = self.shovelHitForce;
		if (!Plugin.ModConfig.ConfigAllowCrits.Value || !self.critPossible) return;

        self.shovelHitForce = ShovelExtensions.CriticalHit(self.shovelHitForce, random, self.critChance);
    }

    static void TryHealNatureMace(ref CodeRebirthWeapons self)
    {
        if (self.playerHeldBy == null || self is not NaturesMace naturesMace || GameNetworkManager.Instance.localPlayerController != self.playerHeldBy) return;

        List<PlayerControllerB> playerList = naturesMace.HitNaturesMace();
        Plugin.ExtendedLogging("playerList: " + playerList.Count);
        foreach (PlayerControllerB player in playerList)
        {
            naturesMace.HealServerRpc(Array.IndexOf(StartOfRound.Instance.allPlayerScripts, player));
        }
    }
}