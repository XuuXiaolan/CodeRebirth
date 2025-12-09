using System.Collections.Generic;
using CodeRebirth.src.MiscScripts;
using CodeRebirth.src.Util;
using Dawn;
using Dusk;
using UnityEngine;

namespace CodeRebirth.src.Content.Unlockables;

public class CRUnlockableUpgradeScrap : UnlockableUpgradeScrap
{
    public override void Start()
    {
        base.Start();

        if (!IsServer)
            return;

        if (!UnlockableReference.TryResolve(out DawnUnlockableItemInfo unlockableItemInfo) || unlockableItemInfo.DawnPurchaseInfo.PurchasePredicate.CanPurchase() is not TerminalPurchaseResult.FailedPurchaseResult)
        {
            List<Item> itemCandidates = new();
            foreach (DawnItemInfo itemInfo in LethalContent.Items.Values)
            {
                if (itemInfo.Key.Namespace != "code_rebirth")
                    continue;

                if (itemInfo.Item == null || itemInfo.Item.spawnPrefab == null || !itemInfo.Item.spawnPrefab.TryGetComponent(out CRUnlockableUpgradeScrap unlockableUpgradeScrap))
                    continue;

                if (itemInfo.Item == this.itemProperties)
                    continue;

                if (!unlockableUpgradeScrap.UnlockableReference.TryResolve(out DawnUnlockableItemInfo unlockableItemInfo2) || unlockableItemInfo2.DawnPurchaseInfo.PurchasePredicate.CanPurchase() is not TerminalPurchaseResult.FailedPurchaseResult)
                    continue;

                itemCandidates.Add(itemInfo.Item);
            }

            if (itemCandidates.Count <= 0)
            {
                List<(Item item, float weight)> newCandidatesOnLevel = new();
                foreach (SpawnableItemWithRarity spawnableItemWithRarity in RoundManager.Instance.currentLevel.spawnableScrap)
                {
                    newCandidatesOnLevel.Add((spawnableItemWithRarity.spawnableItem, spawnableItemWithRarity.rarity));
                }
                Item? chosenItem = CRUtilities.ChooseRandomWeightedType(newCandidatesOnLevel);
                if (chosenItem != null)
                {
                    CodeRebirthUtils.Instance.SpawnScrap(chosenItem, transform.position, false, true, 5);
                }
            }
            else
            {
                Item chosenItem = itemCandidates[Random.Range(0, itemCandidates.Count)];
                CodeRebirthUtils.Instance.SpawnScrap(chosenItem, transform.position, false, true, 0);
            }
            NetworkObject.Despawn();
        }
    }
}