using System.Collections;
using System.Collections.Generic;
using System.Linq;
using CodeRebirth.src.Content.Items;
using CodeRebirth.src.MiscScripts;
using CodeRebirth.src.Util;
using CodeRebirth.src.Util.Extensions;
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
    public Transform SpotLightLeftRight = null!;
    public Transform SpotLightUpDown = null!;
    public MerchantBarrel[] merchantBarrels = [];
    public InteractTrigger walletTrigger = null!;

    private Dictionary<GrabbableObject, int> itemsSpawned = new();
    private List<PlayerControllerB> targetPlayers = new();
    private Dictionary<Transform, float> localDamageCooldownPerTurret = new();
    private int currentCoinsStored = 0;
    public GameObject[] coinObjects = [];
    private bool canTarget = true;
    private NetworkVariable<bool> isActive = new(false, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);
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

    public void Update()
    {
        if (IsServer)
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
        walletTrigger.interactable = GameNetworkManager.Instance.localPlayerController.currentlyHeldObjectServer != null && GameNetworkManager.Instance.localPlayerController.currentlyHeldObjectServer.itemProperties.itemName == "Wallet";
        foreach (KeyValuePair<GrabbableObject, int> item in itemsSpawned.ToArray())
        {
            if (item.Value == -1) continue;
            if (item.Key.isHeld && item.Key.playerHeldBy != null && !targetPlayers.Contains(item.Key.playerHeldBy))
            {
                if (EnoughMoneySlotted(item.Value))
                {
                    itemsSpawned[item.Key] = -1;
                    continue;
                }
                targetPlayers.Add(item.Key.playerHeldBy);
            }
        }
    }

    public void LateUpdate()
    {
        if (targetPlayers.Count <= 0) return;
        EliminateTargetPlayers();
    }

    private bool EnoughMoneySlotted(int itemCost)
    {
        StartCoroutine(StealAllCoins());
        if (itemCost <= currentCoinsStored)
        {
            return true;
        }
        return false;
    }

    private IEnumerator StealAllCoins()
    {
        canTarget = false;
        if (IsServer) networkAnimator.SetTrigger(TakeCoinsAnimation);
        yield return new WaitForSeconds(9f); // wait for animation to end.
        canTarget = true;
        currentCoinsStored = 0;
        DisableOrEnableCoinObjects();
    }

    private void EliminateTargetPlayers()
    {
        if (!canTarget) return;
        var currentTargetPlayer = targetPlayers[0];
        if (currentTargetPlayer.isPlayerDead || !currentTargetPlayer.isPlayerControlled)
        {
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
                if (GameNetworkManager.Instance.localPlayerController == currentTargetPlayer && localDamageCooldownPerTurret[turret] <= 0)
                {
                    currentTargetPlayer.DamagePlayer(2, true, true, CauseOfDeath.Blast, 0, false, currentTargetPlayer.velocityLastFrame);
                    localDamageCooldownPerTurret[turret] = 0.2f;
                }
            }
            Quaternion targetRotation = Quaternion.LookRotation(normalizedDirection);
            turret.rotation = Quaternion.Lerp(turret.rotation, targetRotation, 2f * Time.deltaTime);
        }
    }

    public void PopulateItemsWithRarityList()
    {
        Dictionary<string, Item> itemsByName = StartOfRound.Instance.allItemsList.itemsList
            .ToDictionary(item => item.itemName.ToLowerInvariant().Trim());

        foreach (var barrel in merchantBarrels)
        {
            barrel.validItemsWithRarityAndColor.Clear();

            foreach (var itemNamesWithRarityAndColor in barrel.itemNamesWithRarityAndColor)
            {
                string name = itemNamesWithRarityAndColor.itemName;
                float rarity = itemNamesWithRarityAndColor.rarity;
                int price = itemNamesWithRarityAndColor.price;
                Color borderColor = itemNamesWithRarityAndColor.borderColor;
                Color textColor = itemNamesWithRarityAndColor.textColor;

                string normalizedName = name.ToLowerInvariant().Trim();

                if (itemsByName.TryGetValue(normalizedName, out Item matchingItem))
                {
                    Plugin.ExtendedLogging($"Merchant item: {name}");
                    Plugin.ExtendedLogging($"Merchant item rarity: {rarity}");
                    Plugin.ExtendedLogging($"Merchant item price: {price}");
                    Plugin.ExtendedLogging($"Merchant item border color: {borderColor}");
                    Plugin.ExtendedLogging($"Merchant item text color: {textColor}");
                    Plugin.ExtendedLogging($"Comparable item: {matchingItem.itemName}\n");

                    barrel.validItemsWithRarityAndColor.Add((matchingItem, rarity, price, borderColor, textColor));
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
                Plugin.ExtendedLogging("No valid items for barrel at " + spawnPosition);
                continue;
            }

            var validItems = barrel.validItemsWithRarityAndColor.ToList();
            validItems.Shuffle();

            // Compute cumulative weights.
            float cumulativeWeight = 0;
            var cumulativeList = new List<(Item item, float cumulativeWeight, int price, Color borderColor, Color textColor)>(validItems.Count);
            foreach (var (item, rarity, price, borderColor, textColor) in validItems)
            {
                cumulativeWeight += rarity;
                cumulativeList.Add((item, cumulativeWeight, price, borderColor, textColor));
            }

            // Get a random value in the range [0, cumulativeWeight).
            float randomValue = Random.Range(0, cumulativeWeight);

            Item? selectedItem = null;
            int _price = 0;
            Color _borderColor = Color.white;
            Color _textColor = Color.white;
            foreach (var (item, cumWeight, price, borderColor, textColor) in cumulativeList)
            {
                if (randomValue < cumWeight)
                {
                    selectedItem = item;
                    _price = price;
                    _borderColor = borderColor;
                    _textColor = textColor;
                    break;
                }
            }

            if (selectedItem == null)
            {
                Plugin.ExtendedLogging("Item selection failed for barrel at " + spawnPosition);
                continue;
            }

            // Spawn the selected item.
            GameObject itemGO = (GameObject)CodeRebirthUtils.Instance.SpawnScrap(selectedItem, spawnPosition, false, true, 0);
            GrabbableObject grabbableObject = itemGO.GetComponent<GrabbableObject>();
            // Sync from here:
            itemsSpawned.Add(grabbableObject, _price);
            barrel.textMeshPro.text = _price.ToString();
            ForceScanColorOnItem forceScanColorOnItem = itemGO.AddComponent<ForceScanColorOnItem>();
            forceScanColorOnItem.grabbableObject = grabbableObject;
            forceScanColorOnItem.borderColor = _borderColor;
            forceScanColorOnItem.textColor = _textColor;
        }
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