using CodeRebirth.src.Content.Weapons;
using CodeRebirth.src.Util.Extensions;
using System.Collections.Generic;
using GameNetcodeStuff;
using System;
using CodeRebirth.src.Util;
using CodeRebirth.src.Content.Unlockables;
using UnityEngine;

namespace CodeRebirth.src.Patches;
static class ShovelPatch {
	static Random? random;
	
	public static void Init() {
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
        random ??= new Random(StartOfRound.Instance.randomMapSeed + 85);
        TryCritWeapon(ref self);
        TryHealNatureMace(ref self);
    }

    static void PostHitShovel(ref Shovel self)
    {
        if (self is not CodeRebirthWeapons CRWeapon) return;
        ResetWeaponDamage(ref CRWeapon);
        TryBreakTrees(ref CRWeapon);
    }

    static void ResetWeaponDamage(ref CodeRebirthWeapons CRWeapon)
    {
        CRWeapon.shovelHitForce = CRWeapon.defaultForce;
    }

    static void TryBreakTrees(ref CodeRebirthWeapons CRWeapon)
    {
        if (!CRWeapon.canBreakTrees) return;

		int num = Physics.OverlapSphereNonAlloc(CRWeapon.weaponTip.position, 5f, RoundManager.Instance.tempColliderResults, 33554432, QueryTriggerInteraction.Ignore);
        if (!Plugin.ModConfig.ConfigFarmingEnabled.Value || random.NextFloat(0f, 1f) >= 0.02f || num <= 0) return;
		RoundManager.Instance.DestroyTreeOnLocalClient(CRWeapon.weaponTip.position);
        Plugin.ExtendedLogging("Tree Destroyed with luck");
        CodeRebirthUtils.Instance.SpawnScrap(UnlockableHandler.Instance.PlantPot.Seed, CRWeapon.weaponTip.position, false, true, 5);
    }

    static void TryCritWeapon(ref Shovel self)
	{
		if (self is not CodeRebirthWeapons CRWeapon) return;

        CRWeapon.defaultForce = CRWeapon.shovelHitForce;
		if (!Plugin.ModConfig.ConfigAllowCrits.Value || !CRWeapon.critPossible) return;

        CRWeapon.shovelHitForce = ShovelExtensions.CriticalHit(CRWeapon.shovelHitForce, random, CRWeapon.critChance);
    }

    static void TryHealNatureMace(ref Shovel self)
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