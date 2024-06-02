﻿using System;
using System.Collections;
using System.Linq;
using CodeRebirth.ItemStuff;
using CodeRebirth.Misc;
using CodeRebirth.Util.Extensions;
using GameNetcodeStuff;
using HarmonyLib;
using Unity.Netcode;
using UnityEngine;
using Random = System.Random;

namespace CodeRebirth.MapStuff;

public class ItemCrate : NetworkBehaviour, IHittable {

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
	float digProgress;

	Vector3 originalPosition;
    Random random = new();
	
	void Awake() {
		trigger = GetComponent<InteractTrigger>();
		pickable = GetComponent <Pickable>();
		trigger.onInteractEarly.AddListener(OnInteractEarly);
        trigger.onInteract.AddListener(OnInteract);
        trigger.onStopInteract.AddListener(OnInteractCancel);

		digProgress = random.NextFloat(0.15f, 0.3f);
		originalPosition = transform.position;
		transform.position = originalPosition + (transform.up * digProgress * .5f);
	}

	void Update() {
		trigger.interactable = digProgress == 1;
		if (GameNetworkManager.Instance.localPlayerController.currentlyHeldObjectServer != null && GameNetworkManager.Instance.localPlayerController.currentlyHeldObjectServer.itemProperties.itemName == "Key") {
			trigger.hoverTip = keyHoverTip;
		} else {
			trigger.hoverTip = regularHoverTip;
		}
	}

	void OnInteractEarly(PlayerControllerB playerController)
	{
        slowlyOpeningSFX.Play();
    }
    void OnInteract(PlayerControllerB playerController)
    {
		Open();
        slowlyOpeningSFX.Stop();
    }

    void OnInteractCancel(PlayerControllerB playerController)
	{
		slowlyOpeningSFX.Stop();
	}

	public void Open() {
		
		if (!IsHost) OpenCrateServerRPC();
		else OpenCrate();
	}

	[ServerRpc(RequireOwnership = false)]
	void OpenCrateServerRPC()
    {
        OpenCrate();
    }

	void OpenCrate()
	{
        Item chosenItem = random.NextItem(StartOfRound.Instance.allItemsList.itemsList.Where(item => item.isScrap).ToList());
        GameObject spawned = Instantiate(chosenItem.spawnPrefab, transform.position, Quaternion.Euler(chosenItem.restingRotation), RoundManager.Instance.spawnedScrapContainer);

        spawned.GetComponent<GrabbableObject>().SetScrapValue((int)(random.Next(chosenItem.minValue, chosenItem.maxValue) * RoundManager.Instance.scrapValueMultiplier));
        spawned.GetComponent<NetworkObject>().Spawn(false);

        StartCoroutine(DestoryAfterSound());
        OpenCrateClientRPC();
    }

	[ClientRpc]
	void OpenCrateClientRPC() {
		OpenCrateLocally();
	}

	void OpenCrateLocally() {
		pickable.IsLocked = false;
		openSFX.Play();
		trigger.enabled = false;
		mainRenderer.enabled = false;
		GetComponent<Collider>().enabled = false;
		opened = true;
	}

	IEnumerator DestoryAfterSound() {
		yield return new WaitForSeconds(1);
		yield return new WaitUntil(() => !openSFX.isPlaying);
		Destroy(gameObject);
	}

	public bool Hit(int force, Vector3 hitDirection, PlayerControllerB playerWhoHit = null, bool playHitSFX = false, int hitID = -1) {
		float progressChange = random.NextFloat(0.25f, 0.33f);
		digProgress = Mathf.Min(digProgress + progressChange, 1);
		Plugin.Logger.LogDebug($"ItemCrate was hit! New digProgress: {digProgress}");
		transform.position = originalPosition + (transform.up * digProgress * .5f);
        
		return true; // this bool literally doesn't get used. i have no clue.
	}
}