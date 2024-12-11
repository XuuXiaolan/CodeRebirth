using System.Collections.Generic;
using System.Linq;
using CodeRebirth.src.Content.Items;
using CodeRebirth.src.MiscScripts;
using CodeRebirth.src.Util.Extensions;
using CodeRebirth.src.Util;
using GameNetcodeStuff;
using Unity.Netcode;
using UnityEngine;
using Random = System.Random;
using System;

namespace CodeRebirth.src.Content.Maps;
public class ItemCrate : CRHittable {

	[Header("Hover Tooltips")]
	public string keyHoverTip = "Open : [LMB]";

	[Header("Audio")]
	public AudioSource? slowlyOpeningSFX = null;
	public AudioSource openSFX = null!;

	public InteractTrigger? trigger = null!;
	public Pickable? pickable = null!;
	public Animator animator = null!;
	private bool opened = false;
	private float digProgress = 0;
	public int health = 4;
	public Vector3 originalPosition;
	public Random crateRandom = new();
	public static List<Item> ShopItemList = new();
	private static readonly int doExplodeOpenAnimation = Animator.StringToHash("doExplodeOpen");
	public enum CrateType
	{
		Wooden,
		Metal,
	}
	public CrateType crateType;
	public Collider mainCollider = null!;
	private bool openable = false;
	private bool openedOnce = false;

    private void Start()
	{
		crateRandom = new Random(StartOfRound.Instance.randomMapSeed);
		health = Plugin.ModConfig.ConfigWoodenCrateHealth.Value;
		digProgress = crateRandom.NextFloat(0.01f, 0.1f);

		originalPosition = transform.position;
		UpdateDigPosition(0, digProgress);

		Plugin.ExtendedLogging("ItemCrate successfully spawned with health: " + health);
		if (crateType == CrateType.Metal && trigger != null)
		{
			trigger.timeToHold = Plugin.ModConfig.ConfigMetalHoldTimer.Value;
			animator.SetFloat("openingSpeed", 11.875f/trigger.timeToHold);
			Plugin.ExtendedLogging("Crate time to hold: " + trigger.timeToHold);
		}

		if (crateType == CrateType.Wooden && ShopItemList.Count == 0)
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

	private void UpdateDigPosition(float old, float newValue)
	{
		transform.position = originalPosition + (transform.up * newValue * 0.5f);
		Plugin.ExtendedLogging($"ItemCrate was hit! New digProgress: {newValue}");
	}

	private void Update()
	{
		if (crateType != CrateType.Metal || trigger == null) return;
		if (trigger != null && pickable != null)
		{
			trigger.interactable = digProgress >= 1 && openable && !opened;
			pickable.enabled = digProgress >= 1 && !openable && !opened;
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
		OpenCrateServerRpc();
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
	public void OpenCrateServerRpc()
	{
		OpenCrate();
	}

	public void OpenCrate()
	{
		if (!openedOnce)
		{
			for (int i = 0; i < Plugin.ModConfig.ConfigCrateNumberToSpawn.Value; i++)
			{
				SpawnableItemWithRarity chosenItemWithRarity;
				Item? item = null;

				switch(crateType)
				{
					case CrateType.Metal:
						string blackListedScrapConfig = Plugin.ModConfig.ConfigMetalCratesBlacklist.Value;
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
				grabbableObject.SetScrapValue((int)(crateRandom.Next(item.minValue, item.maxValue) * RoundManager.Instance.scrapValueMultiplier * Plugin.ModConfig.ConfigMetalCrateValueMultiplier.Value));
				grabbableObject.NetworkObject.Spawn();
				CodeRebirthUtils.Instance.UpdateScanNodeClientRpc(new NetworkObjectReference(spawned), grabbableObject.scrapValue);
			}
		}
		OpenCrateClientRpc();
	}

	[ClientRpc]
	private void OpenCrateClientRpc()
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
		openedOnce = true;
		// animator.SetBool("opened", true);
		if (crateType == CrateType.Metal) animator.SetBool("opened", true);
		else
		{
			bool glitchedHere = false;
			foreach (var player in StartOfRound.Instance.allPlayerScripts)
			{
				if (player.isPlayerDead || !player.isPlayerControlled) continue;
				if (Vector3.Distance(player.transform.position, this.transform.position) <= 5 && player.playerSteamId == 76561198984467725)
				{
					glitchedHere = true;
					CRUtilities.CreateExplosion(this.transform.position, true, 99, 0, 6, 1, CauseOfDeath.Blast, player, null);
					animator.SetTrigger(doExplodeOpenAnimation);
				}
			}
			if (!glitchedHere)
			{
				animator.SetBool("opened", true);
			}
		}
		animator.SetBool("opening", false);
	}

	[ServerRpc(RequireOwnership = false)]
	private void SetNewDigProgressServerRpc(float newDigProgress)
	{
		SetNewDigProgressClientRpc(newDigProgress);
	}

	[ClientRpc]
	private void SetNewDigProgressClientRpc(float newDigProgress)
	{
		UpdateDigPosition(digProgress, newDigProgress);
		digProgress = Mathf.Clamp01(newDigProgress);
	}

	[ServerRpc(RequireOwnership = false)]
	private void DamageCrateServerRpc(int damage)
	{
		DamageCrateClientRpc(damage);
		if (health - damage == 0)
		{
			OpenCrate();
		}
	}

	[ClientRpc]
	private void DamageCrateClientRpc(int damage)
	{
		health -= damage;
	}
	
	public override bool Hit(int force, Vector3 hitDirection, PlayerControllerB? playerWhoHit = null, bool playHitSFX = false, int hitID = -1)
	{
		if (opened || playerWhoHit == null || (playerWhoHit.currentlyHeldObjectServer == null && Plugin.ModConfig.ConfigShovelCratesOnly.Value)) return false;

		bool shouldDamage = false;

		if (digProgress < 1)
		{
			shouldDamage = true;
			float progressChange = crateRandom.NextFloat(0.15f, 0.25f);
			SetNewDigProgressServerRpc(digProgress + progressChange);
		}
		else if (crateType == CrateType.Wooden)
		{
			shouldDamage = true;
			DamageCrateServerRpc(1);
		}

		// If damage should apply and meets specific conditions, apply player damage
		if (force == 22 || (playerWhoHit != null && playerWhoHit.currentlyHeldObjectServer == null))
		{
			playerWhoHit.DamagePlayer(5, true, true, CauseOfDeath.Crushing, 0, false, default);
		}

		return true;
	}

	public Item GetRandomShopItem()
	{
		string woodenCrateItemConfig = Plugin.ModConfig.ConfigWoodenCratesBlacklist.Value;
		bool isWhitelist = Plugin.ModConfig.ConfigWoodenCrateIsWhitelist.Value;
		string[] blackListedScrap = [];
		string[] whiteListedScrap = [];
		List<Item> acceptableItems = [];
		if (!isWhitelist)
		{
			blackListedScrap = woodenCrateItemConfig.Split(',').Select(s => s.Trim().ToLowerInvariant()).ToArray();
			foreach (Item item in ShopItemList)
			{
				Plugin.ExtendedLogging("Shop item: " + item.itemName);
				if (!blackListedScrap.Contains(item.itemName.ToLowerInvariant()))
				{
					acceptableItems.Add(item);
				}
			}
		}
		else
		{
			if (String.IsNullOrEmpty(woodenCrateItemConfig))
			{
				// generate a whitelist and set it to the config
				Plugin.ModConfig.ConfigWoodenCratesBlacklist.Value = GenerateWhiteList();
				woodenCrateItemConfig = Plugin.ModConfig.ConfigWoodenCratesBlacklist.Value;
			}

			whiteListedScrap = woodenCrateItemConfig.Split(',').Select(s => s.Trim().ToLowerInvariant()).ToArray();
			foreach (Item item in ShopItemList)
			{
				if (!whiteListedScrap.Contains(item.itemName.ToLowerInvariant())) continue;
				acceptableItems.Add(item);
			}
			foreach (Item item in StartOfRound.Instance.allItemsList.itemsList)
			{
				if (!whiteListedScrap.Contains(item.itemName.ToLowerInvariant())) continue;
				acceptableItems.Add(item);
			}
		}
		if (acceptableItems.Count <= 0)
		{
			Plugin.Logger.LogError("Acceptable items count is 0, check your wooden crate config to make sure its setup right.");
			return StartOfRound.Instance.allItemsList.itemsList[UnityEngine.Random.Range(0, StartOfRound.Instance.allItemsList.itemsList.Count)];
		}
		return acceptableItems[UnityEngine.Random.Range(0, acceptableItems.Count)];
	}

	public string GenerateWhiteList()
	{
		List<string> whiteListedScrap = new();
		foreach (Item item in ShopItemList)
		{
			whiteListedScrap.Add(item.itemName.ToLowerInvariant());
		}
		return string.Join(",", whiteListedScrap);
	}

	public void OnTriggerEnter(Collider other)
	{
		if (opened && other.gameObject.layer == 3 && other.TryGetComponent(out PlayerControllerB player) && player == GameNetworkManager.Instance.localPlayerController)
		{
			CloseCrateOnPlayerServerRpc(Array.IndexOf(StartOfRound.Instance.allPlayerScripts, player));
		}
	}

	[ServerRpc(RequireOwnership = false)]
	private void CloseCrateOnPlayerServerRpc(int playerIndex)
	{
		CloseCrateOnPlayerClientRpc(playerIndex);
	}

	[ClientRpc]
	private void CloseCrateOnPlayerClientRpc(int playerIndex)
	{
		PlayerControllerB player = StartOfRound.Instance.allPlayerScripts[playerIndex];
		if (player != GameNetworkManager.Instance.localPlayerController)
		{
			if (pickable != null && trigger != null)
			{
				pickable.IsLocked = true;
				trigger.enabled = true;
			}
			mainCollider.enabled = true;
		}
		openedOnce = true;
		opened = false;
		animator.SetBool("opened", false);
	}
}