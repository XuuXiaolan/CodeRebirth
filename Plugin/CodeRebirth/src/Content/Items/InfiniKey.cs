using System;
using CodeRebirth.src.Util;
using GameNetcodeStuff;
using UnityEngine;

namespace CodeRebirth.src.Content.Items;
public class InfiniKey : GrabbableObject
{
    private RaycastHit[] cachedHits = new RaycastHit[10];
    private PlayerControllerB? previousPlayerHeldBy = null;
    public override void ItemActivate(bool used, bool buttonDown = true)
    {
        base.ItemActivate(used, buttonDown);
        if (this.playerHeldBy != null)
		{
			this.previousPlayerHeldBy = this.playerHeldBy;
			if (this.playerHeldBy.IsOwner)
			{
				this.playerHeldBy.playerBodyAnimator.SetTrigger("UseHeldItem1");
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
        int numHits = Physics.SphereCastNonAlloc(previousPlayerHeldBy.gameplayCamera.transform.position + previousPlayerHeldBy.gameplayCamera.transform.right * 0.1f, 0.3f, previousPlayerHeldBy.gameplayCamera.transform.forward, cachedHits, 0.75f, CodeRebirthUtils.Instance.collidersAndRoomAndRailingAndInteractableMask, QueryTriggerInteraction.Collide);
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
			/*RoundManager.PlayRandomClip(this.knifeAudio, this.hitSFX, true, 1f, 0, 1000);
			RoundManager.Instance.PlayAudibleNoise(base.transform.position, 17f, 0.8f, 0, false, 0);
			if (hitSurfaceIndex != -1)
			{
				this.knifeAudio.PlayOneShot(StartOfRound.Instance.footstepSurfaces[hitSurfaceIndex].hitSurfaceSFX);
				WalkieTalkie.TransmitOneShotAudio(this.knifeAudio, StartOfRound.Instance.footstepSurfaces[hitSurfaceIndex].hitSurfaceSFX, 1f);
			}
			this.HitShovelServerRpc(hitSurfaceIndex);*/
		}
    }

    public void OnHit(Collider collider)
    {
        if (collider == null) return;
        Plugin.ExtendedLogging($"OnHit: {collider.gameObject.name} with tag {collider.gameObject.tag}");
        if (collider.gameObject.TryGetComponent(out DoorLock doorlock) && doorlock.isLocked)
        {
            doorlock.UnlockDoorServerRpc();
            return;
        }
        if (collider.gameObject.TryGetComponent(out Pickable pickable) && pickable.IsLocked)
        {
            pickable.Unlock();
            return;
        }
    }
}