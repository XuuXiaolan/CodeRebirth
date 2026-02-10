using System;
using System.Collections;
using Dawn.Utils;
using UnityEngine;

namespace CodeRebirth.src.Content.Items;

public class OldBooze : GrabbableObject
{
    private bool drinking = false;

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
        yield return new WaitForSeconds(1f);
        playerHeldBy.DamagePlayer(-50, true, true);
        playerHeldBy.activatingItem = false;
        playerHeldBy.DiscardHeldObject();
        grabbable = false;
        customGrabTooltip = "Empty...";
    }
}