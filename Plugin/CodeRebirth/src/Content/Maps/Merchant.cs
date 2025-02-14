using System.Collections.Generic;
using System.Linq;
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
            barrel.validItemsWithRarity.Clear();

            foreach ((string name, int rarity) in barrel.itemNamesWithRarity)
            {
                string normalizedName = name.ToLowerInvariant().Trim();

                if (itemsByName.TryGetValue(normalizedName, out Item matchingItem))
                {
                    Plugin.ExtendedLogging($"Merchant item: {name}");
                    Plugin.ExtendedLogging($"Merchant item rarity: {rarity}");
                    Plugin.ExtendedLogging($"Comparable item: {matchingItem.itemName}\n");

                    barrel.validItemsWithRarity.Add((matchingItem, rarity));
                }
            }
        }
    }

    public void HandleSpawningMerchantItems()
    {
        foreach (var barrel in merchantBarrels)
        {
            Vector3 spawnPosition = barrel.barrelSpawnPoint.position;

            if (barrel.validItemsWithRarity == null || barrel.validItemsWithRarity.Count == 0)
            {
                Plugin.ExtendedLogging("No valid items for barrel at " + spawnPosition);
                continue;
            }

            var validItems = barrel.validItemsWithRarity.ToList();
            validItems.Shuffle();

            // Compute cumulative weights.
            int cumulativeWeight = 0;
            var cumulativeList = new List<(Item item, int cumulativeWeight)>(validItems.Count);
            foreach (var (item, weight) in validItems)
            {
                cumulativeWeight += weight;
                cumulativeList.Add((item, cumulativeWeight));
            }

            // Get a random value in the range [0, cumulativeWeight).
            int randomValue = Random.Range(0, cumulativeWeight);

            Item? selectedItem = null;
            foreach (var (item, cumWeight) in cumulativeList)
            {
                if (randomValue < cumWeight)
                {
                    selectedItem = item;
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
            itemsSpawned.Add(itemGO.GetComponent<GrabbableObject>(), false);
        }
    }
}