using CodeRebirth.MapStuff;
using HarmonyLib;
using Unity.Netcode;
using UnityEngine;

namespace CodeRebirth.Patches;

[HarmonyPatch(typeof(RoundManager))]
static class RoundManagerPatch {
	[HarmonyPatch(nameof(RoundManager.SpawnOutsideHazards)), HarmonyPostfix]
	public static void SpawnOutsideMapObjects() {
		if (!RoundManager.Instance.IsHost) return;
		if (!Plugin.ModConfig.ConfigItemCrateEnabled.Value) return;
		Plugin.Logger.LogDebug("Spawning crates!!!");
		System.Random random = new();
		int minValue = 0;
		for (int i = 0; i < random.Next(minValue, Mathf.Clamp(Plugin.ModConfig.ConfigCrateAbundance.Value, minValue, 1000)); i++) {
			Vector3 position = RoundManager.Instance.outsideAINodes[random.Next(0, RoundManager.Instance.outsideAINodes.Length)].transform.position;
			Vector3 vector = RoundManager.Instance.GetRandomNavMeshPositionInBoxPredictable(position, 10f, default, random, -1) + (Vector3.up * 2);

			Physics.Raycast(vector, Vector3.down, out RaycastHit hit, 100, StartOfRound.Instance.collidersAndRoomMaskAndDefault);
            
			GameObject spawnedCrate = GameObject.Instantiate(MapObjectHandler.Instance.Crate.ItemCratePrefab, hit.point, Quaternion.identity, RoundManager.Instance.mapPropsContainer.transform);
			spawnedCrate.transform.up = hit.normal;
			spawnedCrate.GetComponent<NetworkObject>().Spawn();
		}
	}

	[HarmonyPatch(nameof(RoundManager.UnloadSceneObjectsEarly)), HarmonyPostfix]
	static void PatchFix_DespawnOldCrates() {
		foreach (ItemCrate crate in GameObject.FindObjectsOfType<ItemCrate>()) {
			crate.NetworkObject.Despawn();
		}
	}
}