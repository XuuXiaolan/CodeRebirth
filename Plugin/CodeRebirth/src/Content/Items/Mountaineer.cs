using GameNetcodeStuff;
using UnityEngine;

namespace CodeRebirth.src.Content.Items;
public class Mountaineer : GrabbableObject
{
    private bool stuckToWall = false;
    private PlayerControllerB? stuckPlayer = null;

    public override void Update()
    {
        base.Update();

        if (stuckPlayer != null && stuckToWall)
        {
            stuckPlayer.ResetFallGravity();
            stuckPlayer.disableMoveInput = true;
            stuckPlayer.activatingItem = true;
            // stuckPlayer.grab
            if (stuckPlayer.jumpCoroutine != null) // this doesn't work when the player lets go of the mouse to discard the item in the middle of a jump lol, find a better way to detect jump start.
            {
                stuckPlayer.activatingItem = false;
                stuckPlayer.disableMoveInput = false;
                if (stuckPlayer == GameNetworkManager.Instance.localPlayerController) MakePlayerGrabObject(stuckPlayer);
                stuckPlayer = null;
                stuckToWall = false;
            }
        }
    }

    private void MakePlayerGrabObject(PlayerControllerB player)
    {
        player.currentlyGrabbingObject = this;
        player.currentlyGrabbingObject.InteractItem();
        if (player.currentlyGrabbingObject.grabbable && player.FirstEmptyItemSlot() != -1)
        {
            player.playerBodyAnimator.SetBool("GrabInvalidated", false);
            player.playerBodyAnimator.SetBool("GrabValidated", false);
            player.playerBodyAnimator.SetBool("cancelHolding", false);
            player.playerBodyAnimator.ResetTrigger("Throw");
            player.SetSpecialGrabAnimationBool(true, null);
            player.isGrabbingObjectAnimation = true;
            player.cursorIcon.enabled = false;
            player.cursorTip.text = "";
            player.twoHanded = player.currentlyGrabbingObject.itemProperties.twoHanded;
            player.carryWeight = Mathf.Clamp(player.carryWeight + (player.currentlyGrabbingObject.itemProperties.weight - 1f), 1f, 10f);
            if (player.currentlyGrabbingObject.itemProperties.grabAnimationTime > 0f)
            {
                player.grabObjectAnimationTime = player.currentlyGrabbingObject.itemProperties.grabAnimationTime;
            }
            else
            {
                player.grabObjectAnimationTime = 0.4f;
            }
            if (!player.isTestingPlayer)
            {
                player.GrabObjectServerRpc(this.NetworkObject);
            }
            if (player.grabObjectCoroutine != null)
            {
                base.StopCoroutine(player.grabObjectCoroutine);
            }
            player.grabObjectCoroutine = base.StartCoroutine(player.GrabObject());
        }
    }

    public override void ItemActivate(bool used, bool buttonDown = true)
    {
        base.ItemActivate(used, buttonDown);

        isBeingUsed = buttonDown;
        playerHeldBy.activatingItem = buttonDown;

        if (!buttonDown)
        {
            stuckPlayer = playerHeldBy;
            stuckToWall = true;
            StartCoroutine(playerHeldBy.waitToEndOfFrameToDiscard());
        }
        // Also maybe instead override fall curve so that it doesnt fall down if you drop it and just sticks
        // pressing Jump key lets you launch up a bit and regrabs the item.
    }

    public override void FallWithCurve()
    {
        if (stuckToWall) return;
        base.FallWithCurve();
    }

    public override void EquipItem()
    {
        base.EquipItem();
        stuckToWall = false;
        stuckPlayer = null;
    }
}