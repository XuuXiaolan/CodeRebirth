using System;
using System.Collections.Generic;
using System.Linq;
using CodeRebirth.src.Content.Maps;
using CodeRebirth.src.Util;
using Unity.Netcode;
using UnityEngine;

namespace CodeRebirth.src.MiscScripts;

public enum CRItemSpawnerType
{
    Vanilla,
    Special,
}

[Serializable]
public class ItemNameWithRarity
{
    public string Name;
    public float Rarity;
}

public class CRItemSpawner : MonoBehaviour
{
    public CRItemSpawnerType spawnerType;
    public float spawnChance;

    [Header("Special Items")]
    public List<ItemNameWithRarity> specialItemNamesWithRarity = new();

    public void Start()
    {
        if (!NetworkManager.Singleton.IsServer) return;
        switch (spawnerType)
        {
            case CRItemSpawnerType.Vanilla:
                DoVanillaItemSpawn();
                break;
            case CRItemSpawnerType.Special:
                DoSpecialItemSpawn();
                break;
        }
    }

    public void DoVanillaItemSpawn()
    {
        if (UnityEngine.Random.Range(0, 100) < spawnChance)
            return;

        Item? item = Merchant.GetRandomVanillaItem(false);
        CodeRebirthUtils.Instance.SpawnScrap(item, transform.position, false, true, 0);
    }

    public void DoSpecialItemSpawn()
    {
        if (UnityEngine.Random.Range(0, 100) < spawnChance)
            return;

        List<(Item item, float rarity)> specialItems = new();
        foreach (var itemNamesWithRarity in specialItemNamesWithRarity)
        {
            Item? itemToAdd = StartOfRound.Instance.allItemsList.itemsList.Where(x => x.itemName == itemNamesWithRarity.Name).FirstOrDefault();
            if (itemToAdd == null)
            {
                Plugin.Logger.LogWarning("Item not found: " + itemNamesWithRarity.Name);
                continue;
            }
            specialItems.Add((itemToAdd, itemNamesWithRarity.Rarity));
        }

        if (specialItems.Count == 0)
            return;

        Item? item = CRUtilities.ChooseRandomWeightedType(specialItems);
        CodeRebirthUtils.Instance.SpawnScrap(item, transform.position, false, true, 0);
    }
}