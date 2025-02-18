using System.Collections.Generic;
using System.Linq;
using CodeRebirth.src.MiscScripts;
using CodeRebirth.src.Util;
using CodeRebirth.src.Util.Extensions;
using GameNetcodeStuff;
using Unity.Netcode;
using UnityEngine;

namespace CodeRebirth.src.Content.Maps;
public class Merchant : NetworkBehaviour
{
    public Transform[] turretBones = [];
    public MerchantBarrel[] merchantBarrels = [];

    private Dictionary<GrabbableObject, bool> itemsSpawned = new();
    private List<PlayerControllerB> targetPlayers = new();
    public void Start()
    {
        if (!IsServer) return;
        PopulateItemsWithRarityList();
        HandleSpawningMerchantItems();
    }

    public void Update()
    {
        foreach (var item in itemsSpawned)
        {
            if (item.Value) continue;
            if (item.Key.isHeld && item.Key.playerHeldBy != null && !targetPlayers.Contains(item.Key.playerHeldBy))
            {
                if (EnoughMoneySlotted())
                {
                    itemsSpawned[item.Key] = true;
                    continue;
                }
                targetPlayers.Add(item.Key.playerHeldBy);
            }
        }

        if (targetPlayers.Count <= 0) return;
        EliminateTargetPlayers();
    }

    private bool EnoughMoneySlotted()
    {
        return true;
    }

    private void EliminateTargetPlayers()
    {
        var currentTargetPlayer = targetPlayers[0];
        if (currentTargetPlayer.isPlayerDead || !currentTargetPlayer.isPlayerControlled)
        {
            targetPlayers.RemoveAt(0);
            return;
        }
        foreach (var turret in turretBones)
        {
            if (Vector3.Dot(currentTargetPlayer.gameplayCamera.transform.position, turret.position) > 0.9f)
            {
                // Fire at player and deal damage.
                currentTargetPlayer.DamagePlayer(2, true, false, CauseOfDeath.Blast, 0, false, currentTargetPlayer.velocityLastFrame);
            }
            else
            {
                Vector3 direction = currentTargetPlayer.gameplayCamera.transform.position - transform.position;
                Quaternion toRotation = Quaternion.FromToRotation(transform.forward, direction);
                transform.rotation = Quaternion.Lerp(transform.rotation, toRotation, 5 * Time.deltaTime);
            }
        }
        // position turrets towards player, and blast the fuck out of the player.
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
                Color color = itemNamesWithRarityAndColor.color;

                string normalizedName = name.ToLowerInvariant().Trim();

                if (itemsByName.TryGetValue(normalizedName, out Item matchingItem))
                {
                    Plugin.ExtendedLogging($"Merchant item: {name}");
                    Plugin.ExtendedLogging($"Merchant item rarity: {rarity}");
                    Plugin.ExtendedLogging($"Merchant item color: {color}");
                    Plugin.ExtendedLogging($"Comparable item: {matchingItem.itemName}\n");

                    barrel.validItemsWithRarityAndColor.Add((matchingItem, rarity, color));
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
            var cumulativeList = new List<(Item item, float cumulativeWeight, Color color)>(validItems.Count);
            foreach (var itemsWithRarityAndColor in validItems)
            {
                cumulativeWeight += itemsWithRarityAndColor.rarity;
                cumulativeList.Add((itemsWithRarityAndColor.item, cumulativeWeight, itemsWithRarityAndColor.color));
            }

            // Get a random value in the range [0, cumulativeWeight).
            float randomValue = Random.Range(0, cumulativeWeight);

            Item? selectedItem = null;
            Color selectedColor = Color.white;
            foreach (var (item, cumWeight, color) in cumulativeList)
            {
                if (randomValue < cumWeight)
                {
                    selectedItem = item;
                    selectedColor = color;
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
            itemsSpawned.Add(grabbableObject, false);
            ForceScanColorOnItem forceScanColorOnItem = itemGO.AddComponent<ForceScanColorOnItem>();
            forceScanColorOnItem.grabbableObject = grabbableObject;
            forceScanColorOnItem.color = selectedColor;
        }
    }
}