﻿﻿﻿using System.Collections.Generic;
using System.Linq;
using CodeRebirth.src.Content.Items;
using CodeRebirth.src.MiscScripts;
using CodeRebirth.src.Util.Extensions;
using CodeRebirth.src.Util;
using GameNetcodeStuff;
using Unity.Netcode;
using UnityEngine;
using Random = System.Random;

namespace CodeRebirth.src.Content.Maps;
public class ItemCrate : CRHittable {

	[Header("Hover Tooltips")]
	public string regularHoverTip = "Hold : [E]";
	public string keyHoverTip = "Open : [LMB]";

	[Header("Audio")]
	public AudioSource? slowlyOpeningSFX = null;
	public AudioSource openSFX = null!;

	public InteractTrigger? trigger = null!;
	public Pickable? pickable = null!;
	public Animator animator = null!;
	private bool opened = false;
	private NetworkVariable<float> digProgress = new(writePerm: NetworkVariableWritePermission.Owner);
	public NetworkVariable<int> health = new(4);
	public Vector3 originalPosition;
	public Random crateRandom = new();
	public static List<Item> ShopItemList = new();
	public enum CrateType
	{
		Wooden,
		Metal,
	}
	public CrateType crateType;
	public Collider mainCollider = null!;
	private bool openable = false;
	
	private void Start()
	{
		health = new(Plugin.ModConfig.ConfigWoodenCrateHealth.Value);
		if (crateType == CrateType.Metal && trigger != null)
		{
			trigger.timeToHold = Plugin.ModConfig.ConfigMetalHoldTimer.Value;
			animator.SetFloat("openingSpeed", 11.875f/trigger.timeToHold);
			Plugin.ExtendedLogging("Crate time to hold: " + trigger.timeToHold);
		}

		digProgress.OnValueChanged += UpdateDigPosition;

		originalPosition = transform.position;
		UpdateDigPosition(0, 0);

		if (crateType == CrateType.Metal && ShopItemList.Count == 0)
		{
			Terminal terminal = FindObjectOfType<Terminal>();
            
			foreach (Item item in StartOfRound.Instance.allItemsList.itemsList)
			{
				if (!item.isScrap && terminal.buyableItemsList.Contains(item))
				{
					ShopItemList.Add(item);
				}
			}
		}
	}

	public override void OnNetworkSpawn()
	{
		if (IsOwner)
		{
			digProgress.Value = crateRandom.NextFloat(0.01f, 0.1f);
		}
	}

	private void UpdateDigPosition(float old, float newValue)
	{
		if (IsOwner)
		{
			transform.position = originalPosition + (transform.up * newValue * 0.5f);
		}

		Plugin.ExtendedLogging($"ItemCrate was hit! New digProgress: {newValue}");
	}

	private void Update()
	{
		if (crateType != CrateType.Metal || trigger == null) return;
		if (GameNetworkManager.Instance.localPlayerController.currentlyHeldObjectServer != null && GameNetworkManager.Instance.localPlayerController.currentlyHeldObjectServer.itemProperties.itemName == "Key")
		{
			trigger.hoverTip = keyHoverTip;
		}
		else
		{
			trigger.hoverTip = regularHoverTip;
		}
		if (trigger != null && pickable != null)
		{
			trigger.interactable = digProgress.Value >= 1 && openable && !opened;
			pickable.enabled = digProgress.Value >= 1 && !openable && !opened;
		}
	}

	public void OnInteractEarly()
	{
		OnInteractEarlyServerRpc();
	}

	[ServerRpc(RequireOwnership = false)]
	private void OnInteractEarlyServerRpc()
	{
		OnInteractEarlyClientRpc();
	}

	[ClientRpc]
	private void OnInteractEarlyClientRpc()
	{
		slowlyOpeningSFX?.Play();
		animator.SetBool("opening", true);
	}

	public void OnInteract(PlayerControllerB player)
	{
		if (opened) return;
		OpenCrateServerRPC();
	}

	public void OnInteractCancel()
	{
		OnInteractCancelServerRpc();
	}

	[ServerRpc(RequireOwnership = false)]
	private void OnInteractCancelServerRpc()
	{
		OnInteractCancelClientRpc();
	}

	[ClientRpc]
	private void OnInteractCancelClientRpc()
	{
		slowlyOpeningSFX?.Stop();
		animator.SetBool("opening", false);
	}

	public void AllowCrateToBeOpened()
	{
		openable = true;
		if (pickable != null) pickable.IsLocked = false;
	}

	[ServerRpc(RequireOwnership = false)]
	public void OpenCrateServerRPC()
	{
		OpenCrate();
	}

	public void OpenCrate()
	{
		for (int i = 0; i < Plugin.ModConfig.ConfigCrateNumberToSpawn.Value; i++)
		{
			SpawnableItemWithRarity chosenItemWithRarity;
			Item? item = null;

			switch(crateType)
			{
				case CrateType.Metal:
					string blackListedScrapConfig = Plugin.ModConfig.ConfigWoodenCratesBlacklist.Value;
					string[] blackListedScrap = [];
					blackListedScrap = blackListedScrapConfig.Split(',').Select(s => s.Trim().ToLowerInvariant()).ToArray();
					List<SpawnableItemWithRarity> acceptableItems = new();
					foreach (SpawnableItemWithRarity spawnableItemWithRarity in RoundManager.Instance.currentLevel.spawnableScrap)
					{
						Plugin.ExtendedLogging("Moon's item pool: " + spawnableItemWithRarity.spawnableItem.itemName);
						if (!blackListedScrap.Contains(spawnableItemWithRarity.spawnableItem.itemName.ToLowerInvariant()))
						{
							acceptableItems.Add(spawnableItemWithRarity);
						}
					}
					chosenItemWithRarity = crateRandom.NextItem(acceptableItems);
					item = chosenItemWithRarity.spawnableItem;
					break;
				case CrateType.Wooden:
					item = GetRandomShopItem();
					break;
			}

			if (item == null || item.spawnPrefab == null) continue;
			GameObject spawned = Instantiate(item.spawnPrefab, transform.position + Vector3.up * 0.6f + Vector3.right * crateRandom.NextFloat(-0.2f, 0.2f) + Vector3.forward * crateRandom.NextFloat(-0.2f, 0.2f), Quaternion.Euler(item.restingRotation), RoundManager.Instance.spawnedScrapContainer);

			GrabbableObject grabbableObject = spawned.GetComponent<GrabbableObject>();
			if (grabbableObject == null)
			{
				Destroy(spawned);
				continue;
			}
			grabbableObject.SetScrapValue((int)(crateRandom.Next(item.minValue + 200, item.maxValue + 200) * RoundManager.Instance.scrapValueMultiplier));
			grabbableObject.NetworkObject.Spawn();
			CodeRebirthUtils.Instance.UpdateScanNodeClientRpc(new NetworkObjectReference(spawned), grabbableObject.scrapValue);
		}
		OpenCrateClientRPC();
	}

	[ClientRpc]
	public void OpenCrateClientRPC()
	{
		OpenCrateLocally();
	}

	public void OpenCrateLocally()
	{
		slowlyOpeningSFX?.Stop();
		if (pickable != null && trigger != null)
		{
			pickable.IsLocked = false;
			trigger.enabled = false;
		}
		openSFX.Play();
		mainCollider.enabled = false;
		opened = true;

		animator.SetBool("opened", true);
		animator.SetBool("opening", false);
	}

	[ServerRpc(RequireOwnership = false)]
	private void SetNewDigProgressServerRPC(float newDigProgress)
	{
		digProgress.Value = Mathf.Clamp01(newDigProgress);
	}

	[ServerRpc(RequireOwnership = false)]
	private void DamageCrateServerRPC(int damage)
	{
		health.Value -= damage;

		if (health.Value <= 0)
		{
			OpenCrate();
		}
	}
	
	public override bool Hit(int force, Vector3 hitDirection, PlayerControllerB? playerWhoHit = null, bool playHitSFX = false, int hitID = -1)
	{
		if (opened || playerWhoHit == null || (playerWhoHit.currentlyHeldObjectServer == null && Plugin.ModConfig.ConfigShovelCratesOnly.Value)) return false; 
		if (digProgress.Value < 1)
		{
			float progressChange = crateRandom.NextFloat(0.15f, 0.25f);
			SetNewDigProgressServerRPC(digProgress.Value + progressChange);
		}
		else if (crateType == CrateType.Wooden)
		{
			DamageCrateServerRPC(1);
		}
		return true;
	}

	public static Item? GetRandomShopItem()
	{
		string blackListedScrapConfig = Plugin.ModConfig.ConfigMetalCratesBlacklist.Value;
		string[] blackListedScrap = [];
		blackListedScrap = blackListedScrapConfig.Split(',').Select(s => s.Trim().ToLowerInvariant()).ToArray();
		List<Item> acceptableItems = [];
		foreach (Item item in ShopItemList)
		{
			Plugin.ExtendedLogging("Shop item: " + item.itemName);
			if (!blackListedScrap.Contains(item.itemName.ToLowerInvariant()))
			{
				acceptableItems.Add(item);
			}
		}
		return acceptableItems[UnityEngine.Random.Range(0, acceptableItems.Count)];
	}	
}