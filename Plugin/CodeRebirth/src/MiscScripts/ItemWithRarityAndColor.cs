using System;
using Dawn.Utils;
using UnityEngine;

namespace CodeRebirth.src.MiscScripts;
[Serializable]
public class ItemWithRarityAndColor
{
    public string itemName = "Item Name";
    public float rarity = 0;
    public int minPrice = 0;
    public int maxPrice = 0;
    public Color borderColor = new(46f / 255f, 180f / 255f, 0, 1f);
    public Color textColor = new(65f / 255f, 1f, 0f, 1f);
}

[Serializable]
public class RealItemWithRarityAndColor(Item? item, float rarity, int minPrice, int maxPrice, Color borderColor, Color textColor)
{
    public Item? item = item;
    public float rarity = rarity;
    public int minPrice = minPrice;
    public int maxPrice = maxPrice;
    public Color borderColor = borderColor;
    public Color textColor = textColor;
}

[Serializable]
public class SimplifiedRealItemWithRarityAndColor(Item? item, float rarity, Color borderColor, Color textColor)
{
    public Item? item = item;
    public float rarity = rarity;
    public Color borderColor = borderColor;
    public Color textColor = textColor;
}

[Serializable]
public class SimplifiedItemWithRarityAndColor
{
    public string itemName;
    public float rarity;
    public Color borderColor;
    public Color textColor;
}