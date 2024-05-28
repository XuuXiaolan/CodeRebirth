using System.Linq;
using CodeRebirth.ItemStuff;
using CodeRebirth.Misc;
using HarmonyLib;
using Unity.Netcode;
using UnityEngine;
using Random = System.Random;

namespace CodeRebirth.MapStuff;

public class ItemCrate : NetworkBehaviour {

	InteractTrigger trigger;
	Pickable pickable;
	
	void Awake() {
		trigger = GetComponent<InteractTrigger>();
		pickable = GetComponent <Pickable>();
	}

	public void Open() {
		pickable.IsLocked = false;
		Plugin.Logger.LogInfo("Opening Item Crate..");

		Random random = new();
		
		Item chosenItem = random.NextItem(StartOfRound.Instance.allItemsList.itemsList.Where(item => item.isScrap).ToList());
		GameObject spawned = Instantiate(chosenItem.spawnPrefab, transform.position, Quaternion.Euler(chosenItem.restingRotation),
			RoundManager.Instance.spawnedScrapContainer);

		spawned.GetComponent<GrabbableObject>().SetScrapValue((int)(random.Next(chosenItem.minValue, chosenItem.maxValue) * RoundManager.Instance.scrapValueMultiplier));
		spawned.GetComponent<NetworkObject>().Spawn(false);
	}
}