using System;
using System.Linq;
using CodeRebirth.src.MiscScripts;
using CodeRebirth.src.Util;
using GameNetcodeStuff;
using Unity.Netcode;
using UnityEngine;

namespace CodeRebirth.src.Content.Unlockables;
public class UnlockShipUnlockable : MonoBehaviour
{
    public InteractTrigger interactTrigger = null!;

    public void Start()
    {
        interactTrigger.onInteract.AddListener(OnInteract);
    }

    private void OnInteract(PlayerControllerB player)
    {
        if (player != GameNetworkManager.Instance.localPlayerController || player.currentlyHeldObjectServer is not UnlockableUpgradeScrap) return;
        UnlockShipUpgradeServerRpc(Array.IndexOf(StartOfRound.Instance.allPlayerScripts, player));
    }

    [ServerRpc(RequireOwnership = false)]
    private void UnlockShipUpgradeServerRpc(int playerIndex)
    {
        UnlockShipUpgradeClientRpc(playerIndex);
    }

    [ClientRpc]
    private void UnlockShipUpgradeClientRpc(int playerIndex)
    {
        PlayerControllerB player = StartOfRound.Instance.allPlayerScripts[playerIndex];
        if (player.currentlyHeldObjectServer is UnlockableUpgradeScrap unlockableUpgradeScrap)
        {
            UnlockableItem unlockableItem = unlockableUpgradeScrap.unlockableItemDef.unlockable;
            int index = ProgressiveUnlockables.unlockableIDs.Keys.ToList().IndexOf(unlockableItem);
            CodeRebirthUtils.Instance.UnlockProgressively(index, playerIndex, false, true, "Assembled Parts", $"Congratulations on finding the parts, Unlocked {ProgressiveUnlockables.unlockableNames[index]}.");
        }
        else
        {
            Plugin.Logger.LogError("UnlockableUpgradeScrap is null, how did you even get here????");
        }
    }
}