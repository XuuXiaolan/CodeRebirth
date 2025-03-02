using System;
using CodeRebirth.src.Content.Unlockables;

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
        Plugin.ExtendedLogging($"Shockwave Gal's shipUnlockableID: {UnlockableHandler.Instance.ShockwaveBot.ShockWaveBotUnlockable.unlockable}");
        
        if (node.shipUnlockableID != -1 && StartOfRound.Instance.unlockablesList.unlockables[node.shipUnlockableID] == UnlockableHandler.Instance.ShockwaveBot.ShockWaveBotUnlockable.unlockable)
        {
            Plugin.ExtendedLogging($"Twas equal, replacing with deny purchase node with a name: {UnlockableHandler.Instance.ShockwaveBot.ShockWaveBotUnlockable.unlockable.unlockableName}");
            if (UnlockableHandler.Instance.ShockwaveBot.ShockWaveBotUnlockable.unlockable.unlockableName == "???")
            {
                UnlockableHandler.Instance.ShockwaveBot.ShockWaveBotUnlockable.unlockable.unlockableName = "SWRD-1";
            }
            else
            {
                UnlockableHandler.Instance.ShockwaveBot.ShockWaveBotUnlockable.unlockable.unlockableName = "???";
            }
            orig(self, UnlockableHandler.Instance.ShockwaveBot.denyPurchaseNode);
            return;
        }
        orig(self, node);
    }
}