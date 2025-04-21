using System.Collections.Generic;
using CodeRebirth.src.Util.AssetLoading;
using LethalLib.Extras;
using UnityEngine;

namespace CodeRebirth.src.Content.Unlockables;

[CreateAssetMenu(fileName = "CRUnlockableDefinition", menuName = "CodeRebirth/CRUnlockableDefinition", order = 1)]
public class CRUnlockableDefinition : CRContentDefinition
{
    public UnlockableItemDef unlockableItemDef;
    public TerminalNode? DenyPurchaseNode;

    public UnlockableItemDef? GetUnlockableByName(string name)
    {
        if (string.IsNullOrEmpty(name)) return null;
        if (unlockableItemDef.unlockable.unlockableName.Contains(name, System.StringComparison.OrdinalIgnoreCase)) return unlockableItemDef;
        return null;
    }
}

public static class CRUnlockableDefinitionExtensions
{
    public static CRUnlockableDefinition? GetCRUnlockableDefinitionWithUnlockableName(this IReadOnlyList<CRUnlockableDefinition> UnlockableDefinitions, string unlockableName)
    {
        foreach (var entry in UnlockableDefinitions)
        {
            if (entry.GetUnlockableByName(unlockableName) != null) return entry;
        }
        return null;
    }
}