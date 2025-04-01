using System.Collections.Generic;
using CodeRebirth.src.Util.AssetLoading;
using UnityEngine;

namespace CodeRebirth.src.Content.Items;

[CreateAssetMenu(fileName = "CRItemDefinition", menuName = "CodeRebirth/CRItemDefinition", order = 1)]
public class CRItemDefinition : CRContentDefinition
{
    public Item item;
    public TerminalNode? terminalNode;

    public Item? GetItemOnName(string itemName)
    {
        if (string.IsNullOrEmpty(itemName)) return null;
        if (item.itemName.ToLowerInvariant().Contains(itemName.ToLowerInvariant())) return item;
        return null;
    }
}

public static class CRItemDefinitionExtensions
{
    public static CRItemDefinition? GetCRItemDefinitionWithItemName(this IReadOnlyList<CRItemDefinition> ItemDefinitions, string itemName)
    {
        foreach (var entry in ItemDefinitions)
        {
            if (entry.GetItemOnName(itemName) != null) return entry;
        }
        return null;
    }
}