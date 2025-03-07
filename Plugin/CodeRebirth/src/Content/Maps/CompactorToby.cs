using CodeRebirth.src.Util;
using GameNetcodeStuff;
using Unity.Netcode;
using UnityEngine;

namespace CodeRebirth.src.Content.Maps;
public class CompactorToby : MonoBehaviour
{
    public Transform outputTransform = null!;

    public void CompactorInteract(PlayerControllerB player)
    {
        if (!player.IsOwner || player.currentlyHeldObjectServer == null) return;
        if (player.currentlyHeldObjectServer.itemProperties.itemName.Contains("Shredded Scraps"))
        {
            int valueOfItem = player.currentlyHeldObjectServer.scrapValue;
            player.DespawnHeldObject();
            TryCompactItemServerRpc(valueOfItem, false);
        }
        else if (player.currentlyHeldObjectServer is RagdollGrabbableObject)
        {
            int valueOfItem = player.currentlyHeldObjectServer.scrapValue;
            player.DespawnHeldObject();
            TryCompactItemServerRpc(valueOfItem+12, true);
        }
    }

    [ServerRpc(RequireOwnership = false)]
    public void TryCompactItemServerRpc(int value, bool deadPlayer)
    {
        if (deadPlayer)
        {
            CodeRebirthUtils.Instance.SpawnScrap(MapObjectHandler.Instance.CompactorToby.FlatDeadPlayerScrap, outputTransform.position, false, true, value);
            return;
        }
        CodeRebirthUtils.Instance.SpawnScrap(MapObjectHandler.Instance.CompactorToby.SallyCubesScrap, outputTransform.position, false, true, value);
    }
}