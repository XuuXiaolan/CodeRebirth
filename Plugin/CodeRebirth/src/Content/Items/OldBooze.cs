using System;
using System.Collections;
using Dawn.Utils;
using GameNetcodeStuff;
using UnityEngine;

namespace CodeRebirth.src.Content.Items;

public class OldBooze : GrabbableObject
{
    [field: SerializeField]
    public AudioSource AudioSource { get; private set; }

    [field: SerializeField]
    public AudioClip DrinkingClip { get; private set; }

    private bool drinking = false;
    private static readonly int UseTZPItemAnimationHash = Animator.StringToHash("useTZPItem");

    public override void Update()
    {
        base.Update();
        if (!drinking)
        {
            return;
        }

        if (StartOfRound.Instance.shipIsLeaving)
        {
            if (playerHeldBy != null)
            {
                if (playerHeldBy.IsLocalPlayer())
                {
                    playerHeldBy.carryWeight = Mathf.Clamp(playerHeldBy.carryWeight - (itemProperties.weight - 1f), 1f, 10f);
                    playerHeldBy.DestroyItemInSlotAndSync(Array.IndexOf(playerHeldBy.ItemSlots, this));
                }
            }
            else if (IsServer)
            {
                NetworkObject.Despawn(true);
            }
        }
    }

    public override void ItemActivate(bool used, bool buttonDown = true)
    {
        base.ItemActivate(used, buttonDown);
        if (drinking)
        {
            return;
        }

        playerHeldBy.activatingItem = true;
        drinking = true;
        StartCoroutine(DrinkBooze());
    }

    private IEnumerator DrinkBooze()
    {
        PlayerControllerB playerDrunk = playerHeldBy;
        AudioSource.PlayOneShot(DrinkingClip);
        float duration = playerDrunk.drunkness;
        playerDrunk.playerBodyAnimator.SetBool(UseTZPItemAnimationHash, true);
        while (duration <= 1)
        {
            duration += Time.deltaTime;
            playerDrunk.drunkness = duration;
            playerDrunk.increasingDrunknessThisFrame = true;
            yield return null;
        }

        playerDrunk.playerBodyAnimator.SetBool(UseTZPItemAnimationHash, false);
        if (playerDrunk.isPlayerDead)
        {
            grabbable = false;
            customGrabTooltip = "Empty...";
            if (playerHeldBy != null && playerHeldBy.IsLocalPlayer())
            {
                playerHeldBy.carryWeight = Mathf.Clamp(playerHeldBy.carryWeight - (itemProperties.weight - 1f), 1f, 10f);
                playerHeldBy.DestroyItemInSlotAndSync(Array.IndexOf(playerHeldBy.ItemSlots, this));
            }
            yield break;
        }
        playerDrunk.DamagePlayer(-50, true, true);
        playerDrunk.activatingItem = false;
        playerDrunk.DiscardHeldObject();
        grabbable = false;
        customGrabTooltip = "Empty...";
    }
}