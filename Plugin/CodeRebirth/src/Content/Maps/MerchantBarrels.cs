using System.Collections.Generic;
using CodeRebirth.src.MiscScripts;
using TMPro;
using UnityEngine;

namespace CodeRebirth.src.Content.Maps;
public class MerchantBarrel : MonoBehaviour
{
    [SerializeField] public List<ItemWithRarityAndColor> itemNamesWithRarityAndColor = new();
    public TextMeshPro textMeshPro = null!;
    public Transform barrelSpawnPoint = null!;

    [HideInInspector] public List<(Item item, float rarity, int price, Color borderColor, Color textColor)> validItemsWithRarityAndColor = new();
}