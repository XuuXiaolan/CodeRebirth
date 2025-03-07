using GameNetcodeStuff;
using Unity.Netcode;
using UnityEngine;

namespace CodeRebirth.src.Content.Maps;
public class KillAndCompactPlayer : MonoBehaviour
{
    public CompactorToby toby = null!;

    public void OnTriggerEnter(Collider other)
    {
        if (other.TryGetComponent(out PlayerControllerB player) && player.IsOwner)
        {
            if (toby.compacting)
            {
                player.KillPlayer(player.transform.position, true, CauseOfDeath.Crushing, 0, default);
                return;
            }
            GrabbableObject[] grabbableObjects = player.ItemSlots;
            int valueOfItems = 0;
            foreach (GrabbableObject grabbableObject in grabbableObjects)
            {
                if (grabbableObject == null || grabbableObject.itemProperties == null) continue;
                if (grabbableObject.itemProperties.itemName.Contains("Shredded Scraps"))
                {
                    valueOfItems += grabbableObject.scrapValue;
                    player.DespawnHeldObject();
                    return;
                }
                else if (grabbableObject is RagdollGrabbableObject)
                {
                    valueOfItems += grabbableObject.scrapValue + 12;
                    player.DespawnHeldObject();
                    return;
                }
            }
            player.DropAllHeldItems();
            foreach (GrabbableObject grabbableObject in grabbableObjects)
            {
                if (grabbableObject == null || grabbableObject.itemProperties == null) continue;
                toby.DespawnItemServerRpc(new NetworkBehaviourReference(grabbableObject));
            }
            player.KillPlayer(player.transform.position, false, CauseOfDeath.Crushing, 0, default);
            toby.TryCompactItemServerRpc(valueOfItems + 10, true, false);
        }
    }
}