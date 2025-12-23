using CodeRebirth.src.Content.Unlockables;
using UnityEngine;

namespace CodeRebirth.src.Content.Maps;
public class MerchantItem : MonoBehaviour
{
    public int Price { get; private set; }
    public GrabbableObject GrabbableObject { get; private set; }
    public bool AttemptedPurchase { get; private set; }
    public bool Purchased { get; private set; }
    public bool CanPurchase(out string rejectionMessage)
    {
        rejectionMessage = "";
        if (MoneyCounter.Instance == null)
        {
            rejectionMessage = "MoneyCounter is null.";
            return false;
        }

        if (MoneyCounter.Instance.MoneyStored() < Price)
        {
            rejectionMessage = "Not enough money.";
            return false;
        }
        return true;
    }

    public void SetGrabbableObject(GrabbableObject grabbableObject)
    {
        GrabbableObject = grabbableObject;
    }

    public void TryPurchaseItem(out bool enrageMerchant)
    {
        enrageMerchant = false;
        if (!CanPurchase(out string rejectionMessage))
        {
            if (AttemptedPurchase && rejectionMessage == "Not enough money.")
            {
                
            }
            AttemptedPurchase = true;
            return;
        }

        PurchaseItem();
    }

    public void PurchaseItem()
    {
        MoneyCounter.Instance!.RemoveMoney(Price);
        AttemptedPurchase = true;
        Purchased = true;
    }

    public void SetPrice(int price)
    {
        Price = price;
    }
}