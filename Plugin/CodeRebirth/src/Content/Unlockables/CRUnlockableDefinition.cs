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
        if (unlockableItemDef.unlockable.unlockableName.ToLowerInvariant().Contains(name.ToLowerInvariant())) return unlockableItemDef;
        return null;
    }
}