namespace CodeRebirth.src.Content.Items;

public class DuckyTube : GrabbableObject
{
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