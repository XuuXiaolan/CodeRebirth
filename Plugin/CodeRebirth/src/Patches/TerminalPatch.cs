using System;
using System.Linq;
using CodeRebirth.src.Content.Unlockables;
using CodeRebirth.src.MiscScripts;

namespace CodeRebirth.src.Patches;
public static class TerminalPatch
{
    public static void Init()
    {
        On.Terminal.LoadNewNodeIfAffordable += Terminal_LoadNewNodeIfAffordable;
    }

    private static void Terminal_LoadNewNodeIfAffordable(On.Terminal.orig_LoadNewNodeIfAffordable orig, Terminal self, TerminalNode node)
    {
        Plugin.ExtendedLogging($"Node's shipUnlockableID: {node.shipUnlockableID}");

        if (node.shipUnlockableID != -1 && ProgressiveUnlockables.unlockableIDs.ContainsKey(StartOfRound.Instance.unlockablesList.unlockables[node.shipUnlockableID]) && !ProgressiveUnlockables.unlockableIDs[StartOfRound.Instance.unlockablesList.unlockables[node.shipUnlockableID]])
        {
            Plugin.ExtendedLogging($"Twas equal, replacing node with deny purchase node.");
            TerminalNode rejectionNode = ProgressiveUnlockables.rejectionNodes[ProgressiveUnlockables.unlockableIDs.Keys.ToList().IndexOf(StartOfRound.Instance.unlockablesList.unlockables[node.shipUnlockableID])];
            orig(self, rejectionNode);
            return;
        }
        orig(self, node);
    }
}