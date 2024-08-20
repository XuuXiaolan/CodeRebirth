using CodeRebirth.src.Content.Weapons;
using CodeRebirth.src.Util.Extensions;
using HarmonyLib;
using System.Collections.Generic;
using GameNetcodeStuff;
using System;

namespace CodeRebirth.src.Patches;
static class ShovelPatch {
	public static System.Random? random;
	
	public static void Init() {
		On.Shovel.HitShovel += Shovel_HitShovel;
    }

    private static void Shovel_HitShovel(On.Shovel.orig_HitShovel orig, Shovel self, bool cancel)
    {
        if (random == null) {
			if (StartOfRound.Instance != null) {
				random = new System.Random(StartOfRound.Instance.randomMapSeed + 85);
			} else {
				random = new System.Random(69);
			}
		}
		if (self is CodeRebirthWeapons CRWeapon) {
			CRWeapon.defaultForce = CRWeapon.shovelHitForce;
			if (CRWeapon.critPossible && Plugin.ModConfig.ConfigAllowCrits.Value) {
				CRWeapon.shovelHitForce = ShovelExtensions.CriticalHit(CRWeapon.shovelHitForce, random, CRWeapon.critChance);
			}
		}

		if (self is NaturesMace naturesMace) {
			List<PlayerControllerB> playerList = naturesMace.HitNaturesMace();
			Plugin.ExtendedLogging("playerList: " + playerList.Count);
			foreach (PlayerControllerB player in playerList) {
				naturesMace.Heal(player);
			}
		}

		orig(self, cancel);

		if (self is CodeRebirthWeapons CRWeaponPost) {
			CRWeaponPost.shovelHitForce = CRWeaponPost.defaultForce;	
			if (CRWeaponPost.canBreakTrees) {
				Plugin.Logger.LogInfo("Tree Destroyed: " + CRWeaponPost.weaponTip.position);
				RoundManager.Instance.DestroyTreeOnLocalClient(CRWeaponPost.weaponTip.position);
			}
		}
    }
}