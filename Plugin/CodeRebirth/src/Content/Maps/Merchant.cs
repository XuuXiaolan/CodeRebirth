using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using CodeRebirth.src.Content.Items;
using CodeRebirth.src.MiscScripts;
using CodeRebirth.src.Util;
using CodeRebirthLib.Util;
using GameNetcodeStuff;
using Unity.Netcode;
using Unity.Netcode.Components;
using UnityEngine;

namespace CodeRebirth.src.Content.Maps;
public class Merchant : NetworkBehaviour
{
    public Animator merchantAnimator = null!;
    public NetworkAnimator networkAnimator = null!;
    public Transform[] turretBones = [];
    public MerchantBarrel[] merchantBarrels = [];
    public InteractTrigger walletTrigger = null!;
    public AudioSource[] TurretAudioSources = [];
    public AudioClip[] TurretAudioClips = [];

    private Dictionary<GrabbableObject, int> itemsSpawned = new();
    private List<PlayerControllerB> targetPlayers = new();
    private Dictionary<Transform, float> localDamageCooldownPerTurret = new();
    private int currentCoinsStored = 0;
    public GameObject[] coinObjects = [];
    private bool canTarget = true;
    private NetworkVariable<bool> isActive = new(false, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);
    private bool playersWhoStoleKilled = true;
    private Coroutine? destroyShipRoutine = null;
    private static readonly int TakeCoinsAnimation = Animator.StringToHash("takeCoins"); // Trigger
    private static readonly int Activated = Animator.StringToHash("activated"); // Bool

    public void Start()
    {
        DisableOrEnableCoinObjects();
        localDamageCooldownPerTurret.Add(turretBones[0], 0.2f);
        localDamageCooldownPerTurret.Add(turretBones[1], 0.2f);
        walletTrigger.onInteract.AddListener(TryDepositCoinsOntoBarrel);
        if (!IsServer) return;
        PopulateItemsWithRarityList();
        HandleSpawningMerchantItems();
    }

    private bool ItemsStolen()
    {
        foreach (var (item, price) in itemsSpawned)
        {
            if (price == -1) continue;
            if (item == null) return true;
            if (item.isInShipRoom) return true;
            if (Vector3.Distance(item.transform.position, StartOfRound.Instance.shipLandingPosition.position) <= 15) return true;
        }
        return false;
    }

    public void Update()
    {
        if (StartOfRound.Instance.shipIsLeaving)
        {
            if (destroyShipRoutine == null && (!playersWhoStoleKilled || ItemsStolen()))
            {
                CodeRebirthUtils.Instance.shipAnimator.overrideController[CodeRebirthUtils.Instance.shipAnimator.originalShipLeaveClip] = CodeRebirthUtils.Instance.ModifiedDangerousShipLeaveAnimation;
                destroyShipRoutine = StartCoroutine(DestroyShip());
            }
            return;
        }
        if (IsServer && targetPlayers.Count <= 0)
        {
            bool playerNearby = false;
            foreach (var player in StartOfRound.Instance.allPlayerScripts)
            {
                if (!player.isPlayerControlled || player.isPlayerDead) continue;
                float distanceToPlayer = Vector3.Distance(transform.position, player.transform.position);
                if (distanceToPlayer > 20f) continue;
                playerNearby = true;
                break;
            }
            if (!playerNearby && isActive.Value && targetPlayers.Count <= 0)
            {
                isActive.Value = false;
                merchantAnimator.SetBool(Activated, false);
            }
            else if (playerNearby && !isActive.Value)
            {
                merchantAnimator.SetBool(Activated, true);
                isActive.Value = true;
            }
        }
        else if (IsServer && !isActive.Value)
        {
            merchantAnimator.SetBool(Activated, true);
            isActive.Value = true;
        }
        walletTrigger.interactable = GameNetworkManager.Instance.localPlayerController.currentlyHeldObjectServer != null && GameNetworkManager.Instance.localPlayerController.currentlyHeldObjectServer.itemProperties.itemName == "Wallet";
        foreach (KeyValuePair<GrabbableObject, int> item in itemsSpawned.ToArray())
        {
            if (item.Value == -1) continue;
            if (item.Key.isHeld && item.Key.playerHeldBy != null && !targetPlayers.Contains(item.Key.playerHeldBy))
            {
                if (EnoughMoneySlotted(item.Value, item.Key))
                {
                    itemsSpawned[item.Key] = -1;
                    continue;
                }
                playersWhoStoleKilled = false;
                targetPlayers.Add(item.Key.playerHeldBy);
            }
        }

        if (targetPlayers.Count <= 0) return;
        EliminateTargetPlayers();
    }

    private bool EnoughMoneySlotted(int itemCost, GrabbableObject itemTaken)
    {
        StartCoroutine(StealAllCoins(itemTaken, itemCost));
        if (itemCost <= currentCoinsStored)
        {
            return true;
        }
        return false;
    }

    private IEnumerator DestroyShip()
    {
        HUDManager.Instance.DisplayTip("Warning", "The Merchant never forgets thieves...\nPrepare for fire", true);
        HUDManager.Instance.ShakeCamera(ScreenShakeType.VeryStrong);
        HUDManager.Instance.ShakeCamera(ScreenShakeType.Long);
        float timeElapsed = 0f;
        while (timeElapsed <= 11)
        {
            EliminateShip();
            timeElapsed += Time.deltaTime;
            yield return null;
        }
        TurretAudioSources[0].transform.position = StartOfRound.Instance.shipDoorNode.position;
        TurretAudioSources[0].PlayOneShot(TurretAudioClips[UnityEngine.Random.Range(0, TurretAudioClips.Length)]);
    }

    private IEnumerator StealAllCoins(GrabbableObject itemTaken, int itemCost)
    {
        canTarget = false;
        foreach (var items in itemsSpawned.ToArray())
        {
            if (items.Key == itemTaken) continue;
            items.Key.grabbable = false;
        }
        if (IsServer) networkAnimator.SetTrigger(TakeCoinsAnimation);
        yield return new WaitForSeconds(10f);
        foreach (var items in itemsSpawned.ToArray())
        {
            if (items.Key == itemTaken) continue;
            items.Key.grabbable = true;
        }
        canTarget = true;
        currentCoinsStored = Math.Clamp(currentCoinsStored - itemCost, 0, 999);
        DisableOrEnableCoinObjects();
    }

    private void EliminateShip()
    {
        Transform targetTransform = StartOfRound.Instance.shipDoorNode;
        foreach (var turret in turretBones)
        {
            localDamageCooldownPerTurret[turret] -= Time.deltaTime;
            Vector3 normalizedDirection = (targetTransform.position - turret.position).normalized;
            // float dotProduct = Vector3.Dot(turret.forward, normalizedDirection);
            // Plugin.ExtendedLogging($"Dot product: {dotProduct}");
            // play the sound and visuals for shooting towards the ship at 0.9f dotproduct or higher.
            Quaternion targetRotation = Quaternion.LookRotation(normalizedDirection);
            turret.rotation = Quaternion.Lerp(turret.rotation, targetRotation, 0.5f * Time.deltaTime);
        }
    }

    private void EliminateTargetPlayers()
    {
        if (!canTarget) return;
        var currentTargetPlayer = targetPlayers[0];
        if (currentTargetPlayer.isPlayerDead || !currentTargetPlayer.isPlayerControlled)
        {
            if (targetPlayers.Count == 1) playersWhoStoleKilled = true;
            targetPlayers.RemoveAt(0);
            return;
        }

        foreach (var turret in turretBones)
        {
            localDamageCooldownPerTurret[turret] -= Time.deltaTime;
            Vector3 normalizedDirection = (currentTargetPlayer.gameplayCamera.transform.position - turret.position).normalized;
            float dotProduct = Vector3.Dot(turret.forward, normalizedDirection);
            // Plugin.ExtendedLogging($"Dot product: {dotProduct}");
            if (dotProduct > 0.9f)
            {
                // Fire at player and deal damage.
                if (localDamageCooldownPerTurret[turret] <= 0)
                {
                    bool blocked = Physics.Linecast(turret.position, currentTargetPlayer.transform.position, out RaycastHit hit, MoreLayerMasks.CollidersAndRoomAndInteractableAndRailingAndEnemiesAndTerrainAndHazardAndVehicleMask, QueryTriggerInteraction.Collide);
                    // Plugin.ExtendedLogging($"Linecast hit {hit.transform.name}");
                    Vector3 explosionPosition = blocked ? hit.point : currentTargetPlayer.transform.position;
                    if (!blocked)
                    {
                        currentTargetPlayer.DamagePlayer(30, true, true, CauseOfDeath.Blast, 0, false, currentTargetPlayer.velocityLastFrame);
                    }
                    CRUtilities.CreateExplosion(explosionPosition, true, 10, 0, 3, 2, currentTargetPlayer, null, 50f);
                    localDamageCooldownPerTurret[turret] = 2f;
                    TurretAudioSources[0].transform.position = explosionPosition;
                    TurretAudioSources[0].PlayOneShot(TurretAudioClips[UnityEngine.Random.Range(0, TurretAudioClips.Length)]);
                }
            }
            Quaternion targetRotation = Quaternion.LookRotation(normalizedDirection);
            turret.rotation = Quaternion.Lerp(turret.rotation, targetRotation, 0.5f * Time.deltaTime);
        }
    }

    public void PopulateItemsWithRarityList()
    {
        List<(Item item, string itemName)> items = new();
        foreach (var item in StartOfRound.Instance.allItemsList.itemsList)
        {
            Plugin.ExtendedLogging($"Item: {item.itemName}");
            bool duplicate = false;
            foreach (var itemAndName in items)
            {
                if (itemAndName.itemName.ToLowerInvariant().Trim() == item.itemName.ToLowerInvariant().Trim())
                {
                    Plugin.Logger.LogError($"Some mod added a duplicate item.... {item.itemName}");
                    duplicate = true;
                    break;
                }
            }
            if (duplicate) continue;
            items.Add((item, item.itemName));
        }
        Dictionary<string, Item> itemsByName = items.Select(item => item.item)
            .ToDictionary(item => item.itemName.ToLowerInvariant().Trim());

        foreach (var barrel in merchantBarrels)
        {
            barrel.validItemsWithRarityAndColor.Clear();

            foreach (var itemNamesWithRarityAndColor in barrel.itemNamesWithRarityAndColor)
            {
                string name = itemNamesWithRarityAndColor.itemName;
                float rarity = itemNamesWithRarityAndColor.rarity;
                int minPrice = itemNamesWithRarityAndColor.minPrice;
                int maxPrice = itemNamesWithRarityAndColor.maxPrice;
                Color borderColor = itemNamesWithRarityAndColor.borderColor;
                Color textColor = itemNamesWithRarityAndColor.textColor;
                if (minPrice > maxPrice)
                {
                    Plugin.ExtendedLogging($"Rodrigo did a stinky with item: {name} min price: {minPrice} max price: {maxPrice}");
                    minPrice = maxPrice;
                }
                string normalizedName = name.ToLowerInvariant().Trim();
                if (normalizedName == "vanilla")
                {
                    Plugin.ExtendedLogging($"Merchant item: {name}");
                    Plugin.ExtendedLogging($"Merchant item rarity: {rarity}");
                    Plugin.ExtendedLogging($"Merchant item min price: {minPrice}");
                    Plugin.ExtendedLogging($"Merchant item max price: {maxPrice}");
                    Plugin.ExtendedLogging($"Merchant item border color: {borderColor}");
                    Plugin.ExtendedLogging($"Merchant item text color: {textColor}");
                    barrel.validItemsWithRarityAndColor.Add((null, rarity, minPrice, maxPrice, borderColor, textColor));
                }
                else if (itemsByName.TryGetValue(normalizedName, out Item matchingItem))
                {
                    Plugin.ExtendedLogging($"Merchant item: {name}");
                    Plugin.ExtendedLogging($"Merchant item rarity: {rarity}");
                    Plugin.ExtendedLogging($"Merchant item price: {minPrice}");
                    Plugin.ExtendedLogging($"Merchant item max price: {maxPrice}");
                    Plugin.ExtendedLogging($"Merchant item border color: {borderColor}");
                    Plugin.ExtendedLogging($"Merchant item text color: {textColor}");
                    Plugin.ExtendedLogging($"Comparable item: {matchingItem.itemName}\n");

                    barrel.validItemsWithRarityAndColor.Add((matchingItem, rarity, minPrice, maxPrice, borderColor, textColor));
                }
            }
        }
    }

    public void HandleSpawningMerchantItems()
    {
        foreach (var barrel in merchantBarrels)
        {
            Vector3 spawnPosition = barrel.barrelSpawnPoint.position;

            if (barrel.validItemsWithRarityAndColor == null || barrel.validItemsWithRarityAndColor.Count == 0)
            {
                Plugin.ExtendedLogging($"No valid items for barrel {barrel.gameObject.name}");
                continue;
            }

            Item? selectedItem = CRUtilities.ChooseRandomWeightedType(barrel.validItemsWithRarityAndColor.Select(x => (x.item, x.rarity)));
            int _price = 0;
            Color _borderColor = Color.white;
            Color _textColor = Color.white;
            foreach (var (item, rarity, minPrice, maxprice, borderColor, textColor) in barrel.validItemsWithRarityAndColor)
            {
                if (selectedItem != item) continue;
                selectedItem = item;
                _price = UnityEngine.Random.Range(minPrice, maxprice + 1);
                _borderColor = borderColor;
                _textColor = textColor;
                break;
            }

            if (selectedItem == null)
            {
                Plugin.ExtendedLogging("Item selection failed for barrel at " + spawnPosition + "Assuming Random item");
                Item item = GetRandomVanillaItem(false);
                selectedItem = item;
            }

            // Spawn the selected item.
            GameObject itemGO = (GameObject)CodeRebirthUtils.Instance.SpawnScrap(selectedItem, spawnPosition, false, true, 0);
            GrabbableObject grabbableObject = itemGO.GetComponent<GrabbableObject>();
            SyncGrabbableObjectScanStuffServerRpc(new NetworkBehaviourReference(grabbableObject), Array.IndexOf(merchantBarrels, barrel), _price, _borderColor.r, _borderColor.g, _borderColor.b, _textColor.r, _textColor.g, _textColor.b);
        }
    }

    public static Item GetRandomVanillaItem(bool excludeShopItems)
    {
        var vanillaItems = LethalLevelLoader.OriginalContent.Items;
        if (excludeShopItems)
        {
            vanillaItems = vanillaItems.Where(x => x.isScrap).ToList();
        }
        int randomIndex = UnityEngine.Random.Range(0, vanillaItems.Count);
        return vanillaItems[randomIndex];
    }

    [ServerRpc(RequireOwnership = false)]
    public void SyncGrabbableObjectScanStuffServerRpc(NetworkBehaviourReference grabbableObject, int barrelRef, int price, float borderColorR, float borderColorG, float borderColorB, float textColorR, float textColorG, float textColorB)
    {
        SyncGrabbableObjectScanStuffClientRpc(grabbableObject, barrelRef, price, borderColorR, borderColorG, borderColorB, textColorR, textColorG, textColorB);
    }

    [ClientRpc]
    public void SyncGrabbableObjectScanStuffClientRpc(NetworkBehaviourReference grabbableObjectRef, int barrelRef, int price, float borderColorR, float borderColorG, float borderColorB, float textColorR, float textColorG, float textColorB)
    {
        GrabbableObject grabbableObject = (GrabbableObject)grabbableObjectRef;
        grabbableObject.grabbable = true;
        itemsSpawned.Add(grabbableObject, price);
        MerchantBarrel barrel = merchantBarrels[barrelRef];
        barrel.textMeshPro.text = price.ToString();
        ForceScanColorOnItem forceScanColorOnItem = grabbableObject.gameObject.AddComponent<ForceScanColorOnItem>();
        forceScanColorOnItem.grabbableObject = grabbableObject;
        forceScanColorOnItem.borderColor = new Color(borderColorR, borderColorG, borderColorB, 1);
        forceScanColorOnItem.textColor = new Color(textColorR, textColorG, textColorB, 1);
    }

    public void TryDepositCoinsOntoBarrel(PlayerControllerB? playerWhoInteracted)
    {
        Plugin.ExtendedLogging("player interacted: " + playerWhoInteracted);
        if (playerWhoInteracted == null || playerWhoInteracted != GameNetworkManager.Instance.localPlayerController || playerWhoInteracted.currentlyHeldObjectServer == null || playerWhoInteracted.currentlyHeldObjectServer is not Wallet wallet) return;
        if (wallet.coinsStored.Value <= 0) return;
        IncreaseCoinsServerRpc(wallet.coinsStored.Value);
        wallet.ResetCoinsServerRpc(0);
    }

    [ServerRpc(RequireOwnership = false)]
    public void IncreaseCoinsServerRpc(int coinIncrease)
    {
        IncreaseCoinsClientRpc(coinIncrease);
    }

    [ClientRpc]
    public void IncreaseCoinsClientRpc(int coinIncrease)
    {
        currentCoinsStored += coinIncrease;
        DisableOrEnableCoinObjects();
    }

    public void DisableOrEnableCoinObjects()
    {
        foreach (GameObject coinObject in coinObjects)
        {
            coinObject.SetActive(false);
        }
        if (currentCoinsStored <= 0) return;

        int objectsToEnable = Mathf.Clamp(currentCoinsStored / 4, 0, coinObjects.Length - 1);

        for (int i = 0; i <= objectsToEnable; i++)
        {
            coinObjects[i].SetActive(true);
        }
    }
}