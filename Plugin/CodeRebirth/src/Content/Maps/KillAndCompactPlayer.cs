using System.Collections.Generic;
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
            bool isFast = true;
            if (toby.compacting)
            {
                player.KillPlayer(player.transform.position, true, CauseOfDeath.Crushing, 0, default);
                return;
            }
            GrabbableObject[] grabbableObjects = player.ItemSlots;
            List<Vector3> vectorPositions = new();
            int valueOfItems = 0;
            foreach (GrabbableObject grabbableObject in grabbableObjects)
            {
                if (grabbableObject == null || grabbableObject.itemProperties == null) continue;
                vectorPositions.Add(grabbableObject.transform.position);
                if (grabbableObject.itemProperties.itemName.Contains("Shredded Scraps"))
                {
                    valueOfItems += grabbableObject.scrapValue;
                    continue;
                }
                else if (grabbableObject is RagdollGrabbableObject)
                {
                    valueOfItems += grabbableObject.scrapValue + 12;
                    continue;
                }

                isFast = false;
                valueOfItems += grabbableObject.scrapValue - 5;
            }
            player.DropAllHeldItems();
            foreach (GrabbableObject grabbableObject in grabbableObjects)
            {
                if (grabbableObject == null || grabbableObject.itemProperties == null) continue;
                toby.DespawnItemServerRpc(new NetworkBehaviourReference(grabbableObject));
            }
            player.KillPlayer(player.transform.position, false, CauseOfDeath.Crushing, 0, default);
            Vector3 randomPosition = vectorPositions[UnityEngine.Random.Range(0, vectorPositions.Count)];
            toby.TryCompactItemServerRpc(randomPosition, valueOfItems + 10, true, false);
        }
    }
}