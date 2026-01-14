using System;
using System.Collections;
using Dawn.Internal;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.Video;

namespace CodeRebirth.src.Content.Items;
public class CreditPad : GrabbableObject
{
    public AudioSource audioPlayer = null!;
    public VideoPlayer videoPlayer = null!;
    public int creditValue = 0;

    public override void ItemActivate(bool used, bool buttonDown = true)
    {
        base.ItemActivate(used, buttonDown);
        IncreaseShipValueServerRpc();
        StartCoroutine(WaitForEndOfFrame());
    }

    private IEnumerator WaitForEndOfFrame()
    {
        yield return new WaitForSeconds(0.1f);
        yield return new WaitUntil(() => !audioPlayer.isPlaying && !videoPlayer.isPlaying);
        if (isHeld || isPocketed)
        {
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

    [ServerRpc(RequireOwnership = false)]
    public void IncreaseShipValueServerRpc()
    {
        int moneyToBe = Mathf.Min(0, TerminalRefs.Instance.groupCredits + creditValue);
        TerminalRefs.Instance.SyncGroupCreditsClientRpc(moneyToBe, TerminalRefs.Instance.numberOfItemsInDropship);
        PlaySoundClientRpc();
    }

    [ClientRpc]
    public void PlaySoundClientRpc()
    {
        audioPlayer.Play();
        videoPlayer.gameObject.SetActive(true);
        if (!videoPlayer.playOnAwake)
        {
            videoPlayer.Play();
        }
    }
}