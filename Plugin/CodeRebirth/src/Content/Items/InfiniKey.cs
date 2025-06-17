using System;
using CodeRebirth.src.Util;
using CodeRebirthLib.Util;
using GameNetcodeStuff;
using Unity.Netcode;
using UnityEngine;

namespace CodeRebirth.src.Content.Items;
public class InfiniKey : GrabbableObject
{
    public AudioSource infiniSource = null!;
    public AudioClip hitNothingSound = null!;
    public AudioClip UnlockSomethingSound = null!;

    private RaycastHit[] cachedHits = new RaycastHit[10];
    private PlayerControllerB? previousPlayerHeldBy = null;
    public override void ItemActivate(bool used, bool buttonDown = true)
    {
        base.ItemActivate(used, buttonDown);
        if (playerHeldBy != null)
        {
            previousPlayerHeldBy = playerHeldBy;
            if (playerHeldBy.IsOwner)
            {
                playerHeldBy.playerBodyAnimator.SetTrigger("UseHeldItem1");
            }
        }
        if (IsOwner)
        {
            UseInfiniKey();
        }
    }

    public void UseInfiniKey()
    {
        if (this.previousPlayerHeldBy == null)
        {
            Plugin.Logger.LogError("Previousplayerheldby is null on this client when HitShovel is called.");
            return;
        }
        previousPlayerHeldBy.activatingItem = false;
        int hitSurfaceIndex = -1;
        this.previousPlayerHeldBy.twoHanded = false;
        int numHits = Physics.SphereCastNonAlloc(previousPlayerHeldBy.gameplayCamera.transform.position + previousPlayerHeldBy.gameplayCamera.transform.right * 0.1f, 0.3f, previousPlayerHeldBy.gameplayCamera.transform.forward, cachedHits, 0.75f, MoreLayerMasks.collidersAndRoomAndRailingAndInteractableMask, QueryTriggerInteraction.Collide);
        for (int i = 0; i < numHits; i++)
        {
            if (cachedHits[i].transform.gameObject.layer == 8 || cachedHits[i].transform.gameObject.layer == 11)
            {
                string tag = cachedHits[i].collider.gameObject.tag;
                hitSurfaceIndex = Array.IndexOf(StartOfRound.Instance.footstepSurfaces, tag);
                continue;
            }
            OnHit(cachedHits[i].collider);
        }
        if (hitSurfaceIndex != -1)
        {
            HitInfiniKeyServerRpc(hitSurfaceIndex);
        }
        else
        {
            HitInfiniKeyServerRpc(-2);
        }
    }

    public void OnHit(Collider collider)
    {
        if (collider == null) return;
        Plugin.ExtendedLogging($"OnHit: {collider.gameObject.name} with tag {collider.gameObject.tag}");
        bool unlockedSomething = false;
        if (collider.gameObject.TryGetComponent(out DoorLock doorlock) && doorlock.isLocked)
        {
            doorlock.UnlockDoorServerRpc();
            unlockedSomething = true;
        }
        else if (collider.gameObject.TryGetComponent(out Pickable pickable))
        {
            pickable.Unlock();
            unlockedSomething = true;
        }
        if (!unlockedSomething) return;
        HitInfiniKeyServerRpc(-1);
    }

    [ServerRpc(RequireOwnership = false)]
    private void HitInfiniKeyServerRpc(int hitSurfaceIndex)
    {
        HitInfiniKeyClientRpc(hitSurfaceIndex);
    }

    [ClientRpc]
    private void HitInfiniKeyClientRpc(int hitSurfaceIndex)
    {
        if (hitSurfaceIndex == -1)
        {
            infiniSource.PlayOneShot(UnlockSomethingSound);
            return;
        }
        else if (hitSurfaceIndex == -2)
        {
            infiniSource.PlayOneShot(hitNothingSound);
            return;
        }
        RoundManager.Instance.PlayAudibleNoise(transform.position, 17f, 0.8f, 0, false, 0);
        infiniSource.PlayOneShot(StartOfRound.Instance.footstepSurfaces[hitSurfaceIndex].hitSurfaceSFX);
        WalkieTalkie.TransmitOneShotAudio(infiniSource, StartOfRound.Instance.footstepSurfaces[hitSurfaceIndex].hitSurfaceSFX, 1f);
    }
}