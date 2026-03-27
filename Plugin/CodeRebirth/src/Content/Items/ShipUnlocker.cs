using System;
using System.Collections;
using System.Collections.Generic;
using Dawn.Internal;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.Video;

namespace CodeRebirth.src.Content.Items;
public class ShipUnlocker : GrabbableObject
{
    public AudioSource audioPlayer = null!;
    public VideoPlayer videoPlayer = null!;

    private bool usedOnce = false;
    public override void ItemActivate(bool used, bool buttonDown = true)
    {
        base.ItemActivate(used, buttonDown);
        if (usedOnce)
        {
            return;
        }

        List<UnlockableItem> unlockableItems = new();
        foreach (UnlockableItem unlockableItem in StartOfRound.Instance.unlockablesList.unlockables)
        {
            if (unlockableItem.hasBeenUnlockedByPlayer || unlockableItem.alreadyUnlocked)
                continue;

            unlockableItems.Add(unlockableItem);
        }

        if (unlockableItems.Count <= 0)
        {
            return;
        }

        UnlockableItem randomItem = unlockableItems[UnityEngine.Random.Range(0, unlockableItems.Count)];
        StartOfRound.Instance.BuyShipUnlockableServerRpc(StartOfRound.Instance.unlockablesList.unlockables.IndexOf(randomItem), TerminalRefs.Instance.groupCredits);
        PlaySoundClientRpc();
        StartCoroutine(WaitForEndOfFrame());
    }

    private IEnumerator WaitForEndOfFrame()
    {
        yield return new WaitForSeconds(0.25f);
        yield return new WaitUntil(() => !audioPlayer.isPlaying && !videoPlayer.isPlaying);
        if (isHeld || isPocketed)
        {
            playerHeldBy.carryWeight = Mathf.Clamp(playerHeldBy.carryWeight - (itemProperties.weight - 1f), 1f, 10f);
            playerHeldBy.DestroyItemInSlotAndSync(Array.IndexOf(playerHeldBy.ItemSlots, this));
        }
        else
        {
            DespawnItemServerRpc();
        }
    }

    [ServerRpc(RequireOwnership = false)]
    public void DespawnItemServerRpc()
    {
        NetworkObject.Despawn();
    }

    [ClientRpc]
    public void PlaySoundClientRpc()
    {
        usedOnce = true;
        audioPlayer.Play();
        videoPlayer.gameObject.SetActive(true);
        if (!videoPlayer.playOnAwake)
        {
            videoPlayer.Play();
        }
    }
}