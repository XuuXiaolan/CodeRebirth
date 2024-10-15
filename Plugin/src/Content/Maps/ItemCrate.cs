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

	[SerializeField]
	[Header("Hover Tooltips")]
	public string regularHoverTip = "Hold : [E]";
	[SerializeField]
	public string keyHoverTip = "Open : [LMB]";

	[Header("Audio")]
	[SerializeField]
	public AudioSource slowlyOpeningSFX = null!;
	[SerializeField]
	public AudioSource openSFX = null!;
	public InteractTrigger trigger = null!;
	public Pickable pickable = null!;
	private Animator animator = null!;
	private List<PlayerControllerB> playersOpeningBox = new List<PlayerControllerB>(); // rework so that it's faster if more players are opening etc
	public Rigidbody crateLid = null!;
	private bool opened = false;
	private NetworkVariable<float> digProgress = new(writePerm: NetworkVariableWritePermission.Owner);
	public NetworkVariable<int> health = new(4);
	public Vector3 originalPosition;
	public Random random = new();
	public static List<Item> ShopItemList = new();
	public enum CrateType {
		Wooden,
		Metal,
		Golden,
	}
	public CrateType crateType;
	
	public void Awake() {
		trigger = GetComponent<InteractTrigger>();
		pickable = GetComponent<Pickable>();
		animator = GetComponent<Animator>();

		health = new(Plugin.ModConfig.ConfigMetalHitNumber.Value); // todo: make this variable.
		trigger.timeToHold = Plugin.ModConfig.ConfigWoodenOpenTimer.Value; // todo: make this variable.
		trigger.onInteractEarly.AddListener(OnInteractEarly);
		trigger.onInteract.AddListener(OnInteract);
		trigger.onStopInteract.AddListener(OnInteractCancel);

		digProgress.OnValueChanged += UpdateDigPosition;

		originalPosition = transform.position;
		UpdateDigPosition(0, 0);

		if (crateType == CrateType.Metal && ShopItemList.Count == 0) {
			Terminal terminal = FindObjectOfType<Terminal>();
            
			foreach (Item item in StartOfRound.Instance.allItemsList.itemsList) {
				if (!item.isScrap && terminal.buyableItemsList.Contains(item)) {
					ShopItemList.Add(item);
				}
			}
		}
	}

	public override void OnNetworkSpawn() {
		if(IsOwner)
			digProgress.Value = random.NextFloat(0.01f, 0.3f);
	}
	
	private void UpdateDigPosition(float old, float newValue) {
		if(IsOwner)
			transform.position = originalPosition + (transform.up * newValue * .5f);

		Plugin.Logger.LogDebug($"ItemCrate was hit! New digProgress: {newValue}");
		switch(crateType)
		{
			case CrateType.Metal:
				{
					pickable.enabled = false;
					break;
				}
			case CrateType.Wooden:
				{
                    trigger.interactable = newValue >= 1;
                    pickable.enabled = trigger.interactable;
					break;
                }
		}
	}

	public void Update()
	{
		if (GameNetworkManager.Instance.localPlayerController.currentlyHeldObjectServer != null && GameNetworkManager.Instance.localPlayerController.currentlyHeldObjectServer.itemProperties.itemName == "Key")
		{
			trigger.hoverTip = keyHoverTip;
		}
		else
		{
			trigger.hoverTip = regularHoverTip;
		}
	}

	public void OnInteractEarly(PlayerControllerB playerController)
	{
		slowlyOpeningSFX.Play();
	}
	
	public void OnInteract(PlayerControllerB playerController)
	{
		if (GameNetworkManager.Instance.localPlayerController != playerController) return;
		Open();
		slowlyOpeningSFX.Stop();
	}

	public void OnInteractCancel(PlayerControllerB playerController)
	{
		slowlyOpeningSFX.Stop();
	}

	public void Open()
	{
		if(opened) return;
		if (!IsHost) OpenCrateServerRPC();
		else OpenCrate();
	}

	[ServerRpc(RequireOwnership = false)]
	public void OpenCrateServerRPC()
	{
		OpenCrate();
	}

	public void OpenCrate() {
		for (int i = 0; i < Plugin.ModConfig.ConfigCrateNumberToSpawn.Value; i++) {
			SpawnableItemWithRarity chosenItemWithRarity;
			Item? item;
			switch(crateType)
			{
				case CrateType.Metal:
                    {
                        item = GetRandomShopItem();
						break;
                    }
				case CrateType.Wooden:
                default:
					{
						string blackListedScrapConfig = Plugin.ModConfig.ConfigWoodenCratesBlacklist.Value;
						string[] blackListedScrap = [];
						blackListedScrap = blackListedScrapConfig.Split(',').Select(s => s.Trim().ToLowerInvariant()).ToArray();
						List<SpawnableItemWithRarity> acceptableItems = new();
						foreach (SpawnableItemWithRarity spawnableItemWithRarity in RoundManager.Instance.currentLevel.spawnableScrap)
						{
							if (!blackListedScrap.Contains(spawnableItemWithRarity.spawnableItem.itemName.ToLowerInvariant()))
							{
								acceptableItems.Add(spawnableItemWithRarity);
							}
						}
						chosenItemWithRarity = random.NextItem(acceptableItems);
						item = chosenItemWithRarity.spawnableItem;
						break;
                    }
			}
			if (item == null || item.spawnPrefab == null) continue;
			GameObject spawned = Instantiate(item.spawnPrefab, transform.position + transform.up*0.6f + transform.right*random.NextFloat(-0.2f, 0.2f) + transform.forward*random.NextFloat(-0.2f, 0.2f), Quaternion.Euler(item.restingRotation), RoundManager.Instance.spawnedScrapContainer);

			GrabbableObject grabbableObject = spawned.GetComponent<GrabbableObject>();
			if (grabbableObject == null) {
				Destroy(spawned);
				continue;
			}
			grabbableObject.SetScrapValue((int)(random.Next(item.minValue + 10, item.maxValue + 10) * RoundManager.Instance.scrapValueMultiplier * (crateType == CrateType.Metal ? 1.2f : 1f)));
			grabbableObject.NetworkObject.Spawn();
			CodeRebirthUtils.Instance.UpdateScanNodeClientRpc(new NetworkObjectReference(spawned), grabbableObject.scrapValue);
		}
		OpenCrateClientRPC();
	}

	[ClientRpc]
	public void OpenCrateClientRPC() {
		OpenCrateLocally();
	}

	public void OpenCrateLocally() {
		pickable.IsLocked = false;
		trigger.enabled = false;
		openSFX.Play();
		GetComponent<Collider>().enabled = false;
		opened = true;

		animator?.SetTrigger("opened");
		if (crateLid == null) return;
		crateLid.isKinematic = false;
		crateLid.useGravity = true;
		crateLid.AddForce(crateLid.transform.up * 200f, ForceMode.Impulse); // during tests this launched the lid not up?
	}

	[ServerRpc(RequireOwnership = false)]
	private void SetNewDigProgressServerRPC(float newDigProgress) {
		digProgress.Value = Mathf.Clamp01(newDigProgress);
	}

	[ServerRpc(RequireOwnership = false)]
	private void DamageCrateServerRPC(int damage) {
		health.Value -= damage;

		if (health.Value <= 0) {
			OpenCrate();
		}
	}
	
	public override bool Hit(int force, Vector3 hitDirection, PlayerControllerB? playerWhoHit = null, bool playHitSFX = false, int hitID = -1) {
		if (opened || playerWhoHit == null) return false;
		if (playerWhoHit != null && playerWhoHit.currentlyHeldObjectServer == null && Plugin.ModConfig.ConfigShovelCratesOnly.Value) return false; 
		if (digProgress.Value < 1) {
			float progressChange = random.NextFloat(0.15f, 0.25f);
			if (IsOwner) {
				digProgress.Value += progressChange;
			} else {
				SetNewDigProgressServerRPC(digProgress.Value + progressChange);
			}
		} else if (crateType == CrateType.Metal) {
			DamageCrateServerRPC(1);
		}
		return true;
	}

	public static Item? GetRandomShopItem()
	{
		string blackListedScrapConfig = Plugin.ModConfig.ConfigMetalCratesBlacklist.Value;
		string[] blackListedScrap = [];
		blackListedScrap = blackListedScrapConfig.Split(',').Select(s => s.Trim().ToLowerInvariant()).ToArray();
		List<Item> acceptableItems = new();
		foreach (Item? item in ShopItemList)
		{
			if (blackListedScrap.Contains(item.itemName.ToLowerInvariant()))
			{
				acceptableItems.Add(item);
			}
		}
		return acceptableItems[UnityEngine.Random.Range(0, acceptableItems.Count)];
	}	
}