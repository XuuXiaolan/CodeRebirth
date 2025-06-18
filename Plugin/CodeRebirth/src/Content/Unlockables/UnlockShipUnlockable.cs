using System;
using System.Linq;
using CodeRebirth.src.Util;
using CodeRebirthLib.ContentManagement.Unlockables;
using CodeRebirthLib.Util;
using GameNetcodeStuff;
using Unity.Netcode;
using static CodeRebirthLib.Util.HUDDisplayTip;

namespace CodeRebirth.src.Content.Unlockables;
public class UnlockShipUnlockable : NetworkBehaviour
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
            if (unlockableItem.TryGetDefinition(out CRUnlockableDefinition? unlockableDefinition) && unlockableDefinition.ProgressiveData != null)
            {
                HUDDisplayTip hUDDisplayTip = new("Assembled Parts", $"Congratulations on finding the parts, Unlocked {unlockableDefinition.ProgressiveData.OriginalName}.", AlertType.Hint);
                unlockableDefinition.ProgressiveData.Unlock(hUDDisplayTip);
            }
            if (unlockableUpgradeScrap.IsOwner) player.DespawnHeldObject();
        }
        else
        {
            Plugin.Logger.LogError("UnlockableUpgradeScrap is null, how did you even get here????");
        }
    }
}