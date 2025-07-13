using System;
using System.Collections;
using GameNetcodeStuff;
using Unity.Netcode;
using UnityEngine;

namespace CodeRebirth.src.Content.Weapons;
public class ScaryShrimp : Shovel
{
    public AudioClip killClip = null!;
    public GameObject particleEffectGameObject = null!;

    private PlayerControllerB lastPlayerHeld = null!;
    [NonSerialized] public NetworkVariable<bool> hitEnemy = new(false, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Owner);
    public override void Start()
    {
        base.Start();
        shovelHitForce = 0;
    }

    public override void EquipItem()
    {
        base.EquipItem();
        lastPlayerHeld = playerHeldBy;
    }

    public override void DiscardItem()
    {
        base.DiscardItem();
        this.grabbable = false;
    }

    public override void OnHitGround()
    {
        base.OnHitGround();
        if (!hitEnemy.Value)
        {
            // var particleEffect = Instantiate(particleEffectGameObject, transform.position, transform.rotation);
            // Destroy(particleEffect, 2f);
            lastPlayerHeld.itemAudio.PlayOneShot(hitSFX[0]);
        }
        if (IsServer) this.NetworkObject.Despawn();
    }

    private IEnumerator DespawnHeldObject(PlayerControllerB playerWhoHit)
    {
        yield return new WaitForSeconds(0.1f);
        Plugin.ExtendedLogging("Despawned held object");
        playerWhoHit.DiscardHeldObject();
    }

    [ServerRpc(RequireOwnership = false)]
    public void PastHitEnemyServerRpc(int enemyHp)
    {
        PastHitEnemyClientRpc(enemyHp);
    }

    [ClientRpc]
    public void PastHitEnemyClientRpc(int enemyHp)
    {
        if (enemyHp - 3 <= 0) lastPlayerHeld.itemAudio.PlayOneShot(killClip);
        if (playerHeldBy.currentlyHeldObjectServer == null)
        {
            Plugin.ExtendedLogging("No held object");
            return;
        }
        GrabbableObject grabbableObject = playerHeldBy.currentlyHeldObjectServer;
        grabbableObject.originalScale = Vector3.zero;
        grabbableObject.transform.localScale = Vector3.zero;
        StartCoroutine(DespawnHeldObject(playerHeldBy));
    }

    [ServerRpc(RequireOwnership = false)]
    public void PastHitPlayerServerRpc(int playerIndex)
    {
        PlayerControllerB playerHit = StartOfRound.Instance.allPlayerScripts[playerIndex];
        PastHitPlayerClientRpc(playerIndex, playerHit.health, playerHit.isPlayerDead);
    }

    [ClientRpc]
    public void PastHitPlayerClientRpc(int playerIndex, int playerHp, bool isDead)
    {
        Plugin.ExtendedLogging($"Hit player {playerIndex} with {playerHp} hp and dead: {isDead}");

        PlayerControllerB playerHit = StartOfRound.Instance.allPlayerScripts[playerIndex];
        if (playerHp - 60 <= 0 || isDead)
        {
            lastPlayerHeld.itemAudio.PlayOneShot(killClip);
        }
        else
        {
            playerHit.drunknessInertia = Mathf.Clamp(playerHit.drunknessInertia + 3f * playerHit.drunknessSpeed, 0.1f, 3f);
            playerHit.increasingDrunknessThisFrame = true;
            playerHit.DropAllHeldItems();
        }
        if (playerHeldBy.currentlyHeldObjectServer == null)
        {
            Plugin.ExtendedLogging("No held object");
            return;
        }
        GrabbableObject grabbableObject = playerHeldBy.currentlyHeldObjectServer;
        grabbableObject.originalScale = Vector3.zero;
        grabbableObject.transform.localScale = Vector3.zero;
        StartCoroutine(DespawnHeldObject(playerHeldBy));
    }
}