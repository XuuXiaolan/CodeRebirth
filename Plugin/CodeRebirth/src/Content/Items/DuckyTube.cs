using UnityEngine;

namespace CodeRebirth.src.Content.Items;

public class DuckyTube : GrabbableObject
{
    public override void Update()
    {
        base.Update();
        if (playerHeldBy == null)
        {
            return;
        }

        if (playerHeldBy.isUnderwater)
        {
            playerHeldBy.externalForceAutoFade += new Vector3(0f, Time.deltaTime, 0f);
            playerHeldBy.ResetFallGravity();
        }
    }
    
    public override void EquipItem()
    {
        base.EquipItem();
        parentObject = playerHeldBy.lowerTorsoCostumeContainerBeltBagOffset.transform;
    }

    public override void PocketItem()
    {
        if (IsOwner)
        {
            playerHeldBy.IsInspectingItem = false;
            playerHeldBy.equippedUsableItemQE = false;
        }
        isPocketed = true;
        parentObject = playerHeldBy.lowerTorsoCostumeContainerBeltBagOffset.transform;
    }
}