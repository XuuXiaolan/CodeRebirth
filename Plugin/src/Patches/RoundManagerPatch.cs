using CodeRebirth.MapStuff;
using HarmonyLib;
using Unity.Netcode;
using UnityEngine;

namespace CodeRebirth.Patches;

[HarmonyPatch(typeof(RoundManager))]
static class RoundManagerPatch {
	[HarmonyPatch(nameof(RoundManager.SpawnOutsideHazards)), HarmonyPostfix]
	public static void SpawnOutsideMapObjects() {
		if(!RoundManager.Instance.IsHost) return;
		if(!Plugin.ModConfig.ConfigItemCrateEnabled.Value) return;
		System.Random random = new();
		for (int i = 0; i < random.Next(0, Mathf.Clamp(Plugin.ModConfig.ConfigCrateAbundance.Value, 0, 1000)); i++) {
			Vector3 position = RoundManager.Instance.outsideAINodes[random.Next(0, RoundManager.Instance.outsideAINodes.Length)].transform.position;
			Vector3 vector = RoundManager.Instance.GetRandomNavMeshPositionInBoxPredictable(position, 10f, default, random, -1) + (Vector3.up * 2);

			Physics.Raycast(vector, Vector3.down, out RaycastHit hit, 100);
            
			GameObject spawnedCrate = GameObject.Instantiate(MapObjectHandler.Instance.Assets.ItemCratePrefab, hit.point, Quaternion.identity, RoundManager.Instance.mapPropsContainer.transform);
			spawnedCrate.transform.up = hit.normal;
			spawnedCrate.GetComponent<NetworkObject>().Spawn();
		}
	}
}