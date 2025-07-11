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
    [SerializeField]
    private bool _spawnOnStart = true;
    [SerializeField]
    private CRItemSpawnerType spawnerType = CRItemSpawnerType.Vanilla;
    [SerializeField]
    private float spawnChance = 0f;
    [SerializeField]
    private List<Transform> spawnSpots = new();

    [Header("Special Items")]
    public List<ItemNameWithRarity> specialItemNamesWithRarity = new();

    public void Start()
    {
        if (spawnSpots.Count == 0)
            spawnSpots.Add(transform);

        if (!_spawnOnStart)
            return;

        if (!NetworkManager.Singleton.IsServer)
            return;

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
        if (UnityEngine.Random.Range(0, 100) >= spawnChance)
            return;

        Item? item = Merchant.GetRandomVanillaItem(false);
        Vector3 spawnPosition = spawnSpots[UnityEngine.Random.Range(0, spawnSpots.Count)].position;
        CodeRebirthUtils.Instance.SpawnScrap(item, spawnPosition, false, true, 0);
    }

    public void DoSpecialItemSpawn()
    {
        if (!NetworkManager.Singleton.IsServer)
            return;

        if (UnityEngine.Random.Range(0, 100) >= spawnChance)
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
        Vector3 spawnPosition = spawnSpots[UnityEngine.Random.Range(0, spawnSpots.Count)].position;
        CodeRebirthUtils.Instance.SpawnScrap(item, spawnPosition, false, true, 0);
    }
}