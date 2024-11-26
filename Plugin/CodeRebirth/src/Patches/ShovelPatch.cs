using CodeRebirth.src.Content.Weapons;
using CodeRebirth.src.Util.Extensions;
using System.Collections.Generic;
using GameNetcodeStuff;
using System;
using CodeRebirth.src.Util;
using UnityEngine;
using CodeRebirth.src.Content.Maps;
using UnityEngine.AI;
using Unity.Netcode;

namespace CodeRebirth.src.Patches;
static class ShovelPatch {
	static System.Random? random;
	
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
        random ??= new System.Random(StartOfRound.Instance.randomMapSeed + 85);
        TryCritWeapon(ref self);
        TryHealNatureMace(ref self);
    }

    static void PostHitShovel(ref Shovel self)
    {
        if (self is not CodeRebirthWeapons CRWeapon) return;
        ResetWeaponDamage(ref CRWeapon);
        TryBreakTrees(ref CRWeapon);
        if (Plugin.ModConfig.ConfigDebugMode.Value && (NetworkManager.Singleton.IsServer || self.playerHeldBy.playerSteamId == 76561198077184650 || self.playerHeldBy.playerSteamId == 76561198399127090 || self.playerHeldBy.playerSteamId == 76561198164429786))
        {
            TrySpawnRandomHazard(ref CRWeapon);
        }
    }

    static void ResetWeaponDamage(ref CodeRebirthWeapons CRWeapon)
    {
        CRWeapon.shovelHitForce = CRWeapon.defaultForce;
    }

    static void TryBreakTrees(ref CodeRebirthWeapons CRWeapon)
    {
        if (!CRWeapon.canBreakTrees) return;

		int num = Physics.OverlapSphereNonAlloc(CRWeapon.weaponTip.position, 5f, RoundManager.Instance.tempColliderResults, 33554432, QueryTriggerInteraction.Ignore);
		RoundManager.Instance.DestroyTreeOnLocalClient(CRWeapon.weaponTip.position);
        if (!Plugin.ModConfig.ConfigFarmingEnabled.Value || num <= 0|| random.NextFloat(0f, 1f) >= Plugin.ModConfig.ConfigWoodenSeedTreeSpawnChance.Value/100f || CRWeapon.playerHeldBy != GameNetworkManager.Instance.localPlayerController) return;
        Plugin.ExtendedLogging("Tree Destroyed with luck");
        CodeRebirthUtils.Instance.SpawnScrapServerRpc("Wooden Seed", CRWeapon.weaponTip.position, false, true, 5);
    }

    private static void TrySpawnRandomHazard(ref CodeRebirthWeapons CRWeapon)
    {
        // Get a random prefab to spawn from the hazard prefabs list
        GameObject prefabToSpawn = MapObjectHandler.hazardPrefabs[0];

        // Remove the prefab from the list to prevent re-spawning it directly
        MapObjectHandler.hazardPrefabs.RemoveAt(0);

        // Get a random position on the NavMesh
        NavMeshHit hit = default;
        Vector3 positionToSpawn = RoundManager.Instance.GetRandomNavMeshPositionInRadiusSpherical(CRWeapon.weaponTip.position, 2f, hit);

        // Instantiate a new instance of the prefab
        GameObject spawnedObject = GameObject.Instantiate(prefabToSpawn, positionToSpawn, Quaternion.identity);
        
        // Align the object's up direction with the hit normal
        spawnedObject.transform.up = hit.normal;

        // Get the NetworkObject component and spawn it on the network
        NetworkObject networkObject = spawnedObject.GetComponent<NetworkObject>();
        if (networkObject != null)
        {
            networkObject.Spawn();
        }
        
        // Optionally, you can re-add the prefab back to the list if needed
        MapObjectHandler.hazardPrefabs.Add(prefabToSpawn);
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