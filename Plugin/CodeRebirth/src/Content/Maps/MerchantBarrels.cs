using System.Collections.Generic;
using UnityEngine;

namespace CodeRebirth.src.Content.Maps;
public class MerchantBarrel : MonoBehaviour
{
    public List<(string itemName, int rarity)> itemNamesWithRarity = new();
    public Transform barrelSpawnPoint = null!;

    [HideInInspector] public List<(Item item, int rarity)> validItemsWithRarity = new();
}