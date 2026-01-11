using System.Collections;
using System.Collections.Generic;
using CodeRebirth.src.Util;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.Video;

namespace CodeRebirth.src.Content.Items;
public class ShipUnlocker : GrabbableObject
{
    public AudioSource audioPlayer = null!;
    public VideoPlayer videoPlayer = null!;

    public override void LateUpdate()
    {
        base.LateUpdate();
        if (playerHeldBy != null && playerHeldBy.inSpecialInteractAnimation)
        {
            playerHeldBy.disableMoveInput = false;
        }
    }

    public override void ItemActivate(bool used, bool buttonDown = true)
    {
        base.ItemActivate(used, buttonDown);
        playerHeldBy.inSpecialInteractAnimation = true;
        Terminal terminal = CodeRebirthUtils.Instance.shipTerminal;
        List<UnlockableItem> unlockableItems = new();
        foreach (var item in StartOfRound.Instance.unlockablesList.unlockables)
        {
            if (item.hasBeenUnlockedByPlayer) continue;
            unlockableItems.Add(item);
        }
        if (unlockableItems.Count <= 0) return;
        UnlockableItem randomItem = unlockableItems[Random.Range(0, unlockableItems.Count)];
        StartOfRound.Instance.BuyShipUnlockableServerRpc(StartOfRound.Instance.unlockablesList.unlockables.IndexOf(randomItem), terminal.groupCredits);
        PlaySoundClientRpc();
        StartCoroutine(WaitForEndOfFrame());
    }

    private IEnumerator WaitForEndOfFrame()
    {
        yield return new WaitUntil(() => !audioPlayer.isPlaying && !videoPlayer.isPlaying);
        playerHeldBy.inSpecialInteractAnimation = false;
        playerHeldBy.DespawnHeldObject();
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