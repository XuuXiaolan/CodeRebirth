using System.Collections.Generic;
using CodeRebirth.src.MiscScripts;
using GameNetcodeStuff;
using Unity.Netcode;
using UnityEngine;

namespace CodeRebirth.src.Content.Items;
public class NitroCrate : GrabbableObject, IHittable
{
    public static List<NitroCrate> nitroCrates = new();

    private bool exploded = false;
    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();
        nitroCrates.Add(this);
    }

    public override void OnNetworkDespawn()
    {
        base.OnNetworkDespawn();
        CRUtilities.CreateExplosion(this.transform.position, true, 999, 0, 15, 100, null, null, 250f);
        nitroCrates.Remove(this);
    }

    [ServerRpc(RequireOwnership = false)]
    public void RequestServerToDespawnServerRpc()
    {
        exploded = true;
        playerHeldBy?.DropAllHeldItems();
        playerHeldBy?.DropAllHeldItemsClientRpc();
        if (!IsSpawned) return;
        this.NetworkObject.Despawn();
    }

    public bool Hit(int force, Vector3 hitDirection, PlayerControllerB? playerWhoHit = null, bool playHitSFX = false, int hitID = -1)
    {
        if (exploded) return false;
        if (!IsSpawned) return false;
        RequestServerToDespawnServerRpc();
        return true;
    }
}