using System;
using System.Collections;
using System.Linq;
using CodeRebirth.ItemStuff;
using CodeRebirth.Misc;
using HarmonyLib;
using Unity.Netcode;
using UnityEngine;
using Random = System.Random;

namespace CodeRebirth.MapStuff;

public class ItemCrate : NetworkBehaviour {

	[SerializeField]
	MeshRenderer mainRenderer;
    
	[SerializeField]
	[Header("Hover Tooltips")]
	string regularHoverTip = "Hold : [E]";
	[SerializeField]
	string keyHoverTip = "Open : [LMB]";

	[Header("Audio")]
	[SerializeField]
	AudioSource slowlyOpeningSFX;

	[SerializeField]
	AudioSource openSFX;
	
	InteractTrigger trigger;
	
	Pickable pickable;

	bool opened;
	
	void Awake() {
		trigger = GetComponent<InteractTrigger>();
		pickable = GetComponent <Pickable>();
	}

	void Update() {
		if (GameNetworkManager.Instance.localPlayerController.currentlyHeldObjectServer != null && GameNetworkManager.Instance.localPlayerController.currentlyHeldObjectServer.itemProperties.itemName ==
			"Key") {
			trigger.hoverTip = keyHoverTip;
		} else {
			trigger.hoverTip = regularHoverTip;
		}
		
		if(trigger.isBeingHeldByPlayer && !slowlyOpeningSFX.isPlaying) slowlyOpeningSFX.Play();
		if(!trigger.isBeingHeldByPlayer && slowlyOpeningSFX.isPlaying) slowlyOpeningSFX.Stop();
	}

	public void Open() {
		OpenCrateLocally();
		OpenCrateServerRPC();
	}

	[ServerRpc(RequireOwnership = false)]
	void OpenCrateServerRPC() {
		OpenCrateClientRPC();
		
		Random random = new();
		
		Item chosenItem = random.NextItem(StartOfRound.Instance.allItemsList.itemsList.Where(item => item.isScrap).ToList());
		GameObject spawned = Instantiate(chosenItem.spawnPrefab, transform.position, Quaternion.Euler(chosenItem.restingRotation),
			RoundManager.Instance.spawnedScrapContainer);

		spawned.GetComponent<GrabbableObject>().SetScrapValue((int)(random.Next(chosenItem.minValue, chosenItem.maxValue) * RoundManager.Instance.scrapValueMultiplier));
		spawned.GetComponent<NetworkObject>().Spawn(false);

		StartCoroutine(DestoryAfterSound());
	}

	[ClientRpc]
	void OpenCrateClientRPC() {
		OpenCrateLocally();
	}

	void OpenCrateLocally() {
		if(opened) return;
		
		pickable.IsLocked = false;
		openSFX.Play();
		trigger.enabled = false;
		mainRenderer.enabled = false;
		GetComponent<Collider>().enabled = false;
		opened = true;
	}

	IEnumerator DestoryAfterSound() {
		yield return new WaitUntil(() => !openSFX.isPlaying);
		Destroy(gameObject);
	}
}