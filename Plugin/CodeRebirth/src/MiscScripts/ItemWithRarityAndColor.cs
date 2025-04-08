using System;
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