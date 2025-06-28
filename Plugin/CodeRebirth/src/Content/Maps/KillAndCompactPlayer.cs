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
                if (grabbableObject == null || grabbableObject.itemProperties == null)
                    continue;

                vectorPositions.Add(grabbableObject.transform.position);
                valueOfItems += grabbableObject.scrapValue;
            }
            player.DropAllHeldItems();
            foreach (GrabbableObject grabbableObject in grabbableObjects)
            {
                if (grabbableObject == null || grabbableObject.itemProperties == null)
                    continue;

                toby.DespawnItemServerRpc(new NetworkBehaviourReference(grabbableObject));
            }
            player.KillPlayer(player.transform.position, false, CauseOfDeath.Crushing, 0, default);
            Vector3 randomPosition = vectorPositions[UnityEngine.Random.Range(0, vectorPositions.Count)];
            toby.TryCompactItemServerRpc(randomPosition, valueOfItems, player, true);
        }
    }
}