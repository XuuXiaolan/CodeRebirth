using System.Collections;
using CodeRebirth.src.Util;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.Video;

namespace CodeRebirth.src.Content.Items;
public class CreditPad : GrabbableObject
{
    public AudioSource audioPlayer = null!;
    public VideoPlayer videoPlayer = null!;
    public int creditValue = 0;

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
        IncreaseShipValueServerRpc();
        StartCoroutine(WaitForEndOfFrame());
    }

    private IEnumerator WaitForEndOfFrame()
    {
        yield return new WaitForSeconds(0.1f);
        yield return new WaitUntil(() => !audioPlayer.isPlaying && !videoPlayer.isPlaying);
        playerHeldBy.inSpecialInteractAnimation = false;
        playerHeldBy.DespawnHeldObject();
    }

    [ServerRpc(RequireOwnership = false)]
    public void IncreaseShipValueServerRpc()
    {
        Terminal? terminal = CodeRebirthUtils.Instance.shipTerminal;
        if (terminal == null) return;
        int moneyToBe = terminal.groupCredits + creditValue;
        if (moneyToBe < 0) moneyToBe = 0;
        terminal.SyncGroupCreditsClientRpc(moneyToBe, terminal.numberOfItemsInDropship);
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