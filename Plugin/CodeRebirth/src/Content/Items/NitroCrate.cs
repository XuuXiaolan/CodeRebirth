using System.Collections;
using System.Collections.Generic;
using CodeRebirth.src.MiscScripts;
using GameNetcodeStuff;
using Unity.Netcode;
using UnityEngine;

namespace CodeRebirth.src.Content.Items;
public class NitroCrate : GrabbableObject, IHittable
{
    public Renderer renderer = null!;
    public static List<NitroCrate> nitroCrates = new();

    [HideInInspector]
    public bool exploded = false;

    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();
        nitroCrates.Add(this);
    }

    public override void OnNetworkDespawn()
    {
        base.OnNetworkDespawn();
        nitroCrates.Remove(this);
    }

    [ServerRpc(RequireOwnership = false)]
    public void RequestServerToDespawnServerRpc()
    {
        if (exploded) return;
        exploded = true;
        playerHeldBy?.DropAllHeldItems();
        playerHeldBy?.DropAllHeldItemsClientRpc();
        StartCoroutine(DespawnAfterDelay(1f));
    }

    private IEnumerator DespawnAfterDelay(float delay)
    {
        DisableRendererAndExplodeClientRpc();
        yield return new WaitForSeconds(delay);
        this.NetworkObject.Despawn();
    }

    [ClientRpc]
    private void DisableRendererAndExplodeClientRpc()
    {
        CRUtilities.CreateExplosion(this.transform.position, true, 999, 0, 15, 100, null, null, 250f);
        renderer.enabled = false;
    }

    public bool Hit(int force, Vector3 hitDirection, PlayerControllerB? playerWhoHit = null, bool playHitSFX = false, int hitID = -1)
    {
        RequestServerToDespawnServerRpc();
        return true;
    }
}