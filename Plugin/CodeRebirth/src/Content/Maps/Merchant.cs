using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Dawn;
using Dusk;
using Dawn.Utils;
using Unity.Netcode;
using UnityEngine;
using CodeRebirth.src.Content.Unlockables;
using CodeRebirth.src.MiscScripts;
using CodeRebirth.src.Util;
using GameNetcodeStuff;

namespace CodeRebirth.src.Content.Maps;

public class Merchant : NetworkBehaviour
{
    public Animator merchantAnimator = null!;
    [SerializeField]
    public List<MerchantBarrel> merchantBarrelPrefabs = new();
    [SerializeField]
    public List<Transform> merchantBarrelSpawns = new();
    [SerializeField]
    public BugleBoy bugleBoy = null!;
    [SerializeField]
    public MerchantTipPad tipPad = null!;
    [SerializeField]
    public AnimationClip PurchaseAnimation = null!;
    [SerializeField]
    public AnimationClip StealAnimation = null!;

    internal System.Random storeSeededRandom = new();
    private Dictionary<GrabbableObject, int> itemsOnSale = new();

    private static readonly int IdleRandomHash = Animator.StringToHash("idleRandom"); // Trigger
    internal static readonly int RerollHash = Animator.StringToHash("reroll"); // Trigger
    private static readonly int StealHash = Animator.StringToHash("steal"); // Trigger
    private static readonly int PurchaseHash = Animator.StringToHash("purchase"); // Trigger
    private static readonly int ActivatedHash = Animator.StringToHash("activated"); // Bool

    public void Start()
    {
        storeSeededRandom = new System.Random(StartOfRound.Instance.randomMapSeed + 37325);
        if (!IsServer)
        {
            return;
        }

        PopulateItemsWithRarityList();
    }

    [ServerRpc(RequireOwnership = false)]
    internal void RerollServerRpc()
    {
        RerollClientRpc();
    }

    [ClientRpc]
    private void RerollClientRpc()
    {
        bugleBoy.animator.SetBool(BugleBoy.ActivatedHash, true);
        StartCoroutine(StopHisSinging());
        merchantAnimator.SetTrigger(RerollHash);
        MoneyCounter.Instance!.RemoveMoney(2);
        foreach (MerchantBarrel merchantBarrel in existingMerchantBarrels)
        {
            if (merchantBarrel.currentlySpawnedGrabbableObject != null)
            {
                merchantBarrel.currentlySpawnedGrabbableObject.grabbable = false;
            }
        }
    }

    public void DeleteItemsAtBarrel(int barrelRef)
    {
        MerchantBarrel merchantBarrel = existingMerchantBarrels[barrelRef];
        if (merchantBarrel.currentlySpawnedGrabbableObject == null)
            return;

        merchantBarrel.currentlySpawnedGrabbableObject.NetworkObject.Despawn();
    }

    public void SpawnItemAtBarrel(int barrelRef)
    {
        MerchantBarrel merchantBarrel = existingMerchantBarrels[barrelRef];
        HandleSpawningMerchantItems(merchantBarrel);
    }

    private IEnumerator StopHisSinging()
    {
        yield return new WaitUntil(() => !bugleBoy.bugleSource.isPlaying);
        bugleBoy.chosenClip = bugleBoy.bugleClips[storeSeededRandom.Next(0, bugleBoy.bugleClips.Length)];
        bugleBoy.rerollTrigger.cooldownTime = bugleBoy.chosenClip.length;
        bugleBoy.animator.SetBool(BugleBoy.ActivatedHash, false);
    }

    private bool previouslyActivated = false;
    public void Update()
    {
        bool playersNearby = false;
        foreach (PlayerControllerB playerControllerB in StartOfRound.Instance.allPlayerScripts)
        {
            if (playerControllerB.isPlayerDead || !playerControllerB.isPlayerControlled)
                continue;

            if (Vector3.Distance(transform.position, playerControllerB.transform.position) <= 30f * (previouslyActivated ? 1.5f : 1f))
            {
                playersNearby = true;
                break;
            }
        }

        if (playersNearby != previouslyActivated)
        {
            merchantAnimator.SetBool(ActivatedHash, playersNearby);
        }
        previouslyActivated = playersNearby;

        for (int i = itemsOnSale.Count - 1; i >= 0; i--)
        {
            GrabbableObject grabbableObject = itemsOnSale.ElementAt(i).Key;
            int price = itemsOnSale.ElementAt(i).Value;
            if (grabbableObject.isHeld && grabbableObject.playerHeldBy != null)
            {
                itemsOnSale.Remove(grabbableObject);
                foreach (MerchantBarrel merchantBarrel in existingMerchantBarrels)
                {
                    if (merchantBarrel.currentlySpawnedGrabbableObject == grabbableObject)
                    {
                        merchantBarrel.currentlySpawnedGrabbableObject = null;
                    }
                }
                grabbableObject.customGrabTooltip = "";
                if (EnoughMoneySlotted(price))
                {
                    StartCoroutine(PayUpBoy(price));
                    if (grabbableObject.playerHeldBy.IsLocalPlayer())
                    {
                        DuskModContent.Achievements.TryTriggerAchievement(CodeRebirthAchievementKeys.Capitalism);
                    }

                    if (itemsOnSale.Count <= 0)
                    {
                        DuskModContent.Achievements.TryTriggerAchievement(CodeRebirthAchievementKeys.OutOfStock);
                    }
                    continue;
                }
                else
                {
                    Steal();
                }
            }
        }
    }

    private bool EnoughMoneySlotted(int itemCost)
    {
        if (MoneyCounter.Instance == null)
        {
            return false;
        }

        if (MoneyCounter.Instance.MoneyStored() < itemCost)
        {
            return false;
        }

        return true;
    }

    private IEnumerator PayUpBoy(int itemCost)
    {
        MoneyCounter.Instance!.RemoveMoney(itemCost);
        foreach ((GrabbableObject grabbableObject, int price) in itemsOnSale)
        {
            SetupGrabbableTooltip(grabbableObject, price);
        }

        foreach (GrabbableObject grabbableObject in itemsOnSale.Keys)
        {
            grabbableObject.grabbable = false;
        }

        merchantAnimator.SetTrigger(PurchaseHash);
        yield return new WaitForSeconds(PurchaseAnimation.length);
        foreach (GrabbableObject grabbableObject in itemsOnSale.Keys)
        {
            grabbableObject.grabbable = true;
        }
    }

    private void Steal()
    {
        foreach (GrabbableObject grabbableObject in itemsOnSale.Keys)
        {
            grabbableObject.grabbable = false;
        }

        bugleBoy.DisableSelf();
        tipPad.CloseDonations();
        merchantAnimator.SetTrigger(StealHash);
        if (IsServer)
        {
            MoneyCounter.Instance!.RemoveMoney(999);
        }
    }

    public void PopulateItemsWithRarityList()
    {
        Dictionary<string, Item> itemsByName = new();
        foreach (Item item in StartOfRound.Instance.allItemsList.itemsList)
        {
            if (item.spawnPrefab != null && item.spawnPrefab.TryGetComponent(out CRUnlockableUpgradeScrap unlockableUpgradeScrap))
            {
                if (!unlockableUpgradeScrap.UnlockableReference.TryResolve(out DawnUnlockableItemInfo unlockableItemInfo) || unlockableItemInfo.DawnPurchaseInfo.PurchasePredicate.CanPurchase() is not TerminalPurchaseResult.FailedPurchaseResult)
                {
                    continue;
                }
            }

            Plugin.ExtendedLogging($"Item: {item.itemName}");
            string normalizedItemName = item.itemName.ToLowerInvariant().Trim();
            if (!itemsByName.TryAdd(normalizedItemName, item))
            {
                Plugin.ExtendedLogging($"Previous item was a duplicate");
            }
        }

        for (int i = 0; i < merchantBarrelPrefabs.Count; i++)
        {
            MerchantBarrel realBarrel = GameObject.Instantiate(merchantBarrelPrefabs[i], merchantBarrelSpawns[i]);
            realBarrel.GetComponent<NetworkObject>().Spawn(true);
            existingMerchantBarrels.Add(realBarrel);
            foreach (ItemWithRarityAndColor itemNamesWithRarityAndColor in realBarrel.itemNamesWithRarityAndColor)
            {
                string name = itemNamesWithRarityAndColor.itemName;
                float rarity = itemNamesWithRarityAndColor.rarity;
                int minPrice = itemNamesWithRarityAndColor.minPrice;
                int maxPrice = itemNamesWithRarityAndColor.maxPrice;
                Color borderColor = itemNamesWithRarityAndColor.borderColor;
                Color textColor = itemNamesWithRarityAndColor.textColor;
                if (minPrice > maxPrice)
                {
                    minPrice = maxPrice;
                }

                string normalizedName = name.ToLowerInvariant().Trim();
                if (normalizedName == "vanilla")
                {
                    Plugin.ExtendedLogging($"Merchant item: {name}");
                    Plugin.ExtendedLogging($"Merchant item rarity: {rarity}");
                    Plugin.ExtendedLogging($"Merchant item min price: {minPrice}");
                    Plugin.ExtendedLogging($"Merchant item max price: {maxPrice}");
                    Plugin.ExtendedLogging($"Merchant item border color: {borderColor}");
                    Plugin.ExtendedLogging($"Merchant item text color: {textColor}");
                    realBarrel.validItemsWithRarityAndColor.Add(new RealItemWithRarityAndColor(null, rarity, minPrice, maxPrice, borderColor, textColor));
                }
                else if (itemsByName.TryGetValue(normalizedName, out Item matchingItem))
                {
                    Plugin.ExtendedLogging($"Merchant item: {name}");
                    Plugin.ExtendedLogging($"Merchant item rarity: {rarity}");
                    Plugin.ExtendedLogging($"Merchant item price: {minPrice}");
                    Plugin.ExtendedLogging($"Merchant item max price: {maxPrice}");
                    Plugin.ExtendedLogging($"Merchant item border color: {borderColor}");
                    Plugin.ExtendedLogging($"Merchant item text color: {textColor}");
                    Plugin.ExtendedLogging($"Comparable item: {matchingItem.itemName}\n");
                    realBarrel.validItemsWithRarityAndColor.Add(new RealItemWithRarityAndColor(matchingItem, rarity, minPrice, maxPrice, borderColor, textColor));
                }
            }

            HandleSpawningMerchantItems(realBarrel);
        }
    }

    internal List<MerchantBarrel> existingMerchantBarrels = new();
    public void HandleSpawningMerchantItems(MerchantBarrel merchantBarrel)
    {
        Vector3 spawnPosition = merchantBarrel.barrelSpawnPoint.position;

        if (merchantBarrel.validItemsWithRarityAndColor == null || merchantBarrel.validItemsWithRarityAndColor.Count == 0)
        {
            Plugin.ExtendedLogging($"No valid items for barrel {merchantBarrel.gameObject.name}");
            return;
        }

        RealItemWithRarityAndColor selectedItem = CRUtilities.ChooseRandomWeightedType(merchantBarrel.validItemsWithRarityAndColor.Select(x => (x, x.rarity)))!;

        if (selectedItem.item == null)
        {
            Plugin.ExtendedLogging("Item selection failed for barrel at " + spawnPosition + "Assuming Random item");
            Item item = GetRandomVanillaItem(false, storeSeededRandom);
            selectedItem.item = item;
        }

        // Spawn the selected item.
        GameObject itemGO = (GameObject)CodeRebirthUtils.Instance.SpawnScrap(selectedItem.item, spawnPosition, false, true, 0);
        GrabbableObject grabbableObject = itemGO.GetComponent<GrabbableObject>();
        SyncGrabbableObjectScanStuffServerRpc(new NetworkBehaviourReference(grabbableObject), existingMerchantBarrels.IndexOf(merchantBarrel), UnityEngine.Random.Range(selectedItem.minPrice, selectedItem.maxPrice + 1), selectedItem.borderColor.r, selectedItem.borderColor.g, selectedItem.borderColor.b, selectedItem.textColor.r, selectedItem.textColor.g, selectedItem.textColor.b);
    }

    public static Item GetRandomVanillaItem(bool excludeShopItems, System.Random? storeSeededRandom = null)
    {
        storeSeededRandom ??= new System.Random(UnityEngine.Random.Range(0, 1000000));
        var vanillaItems = LethalContent.Items.Values.Where(x => x.Key.IsVanilla()).ToList();
        if (excludeShopItems)
        {
            vanillaItems = vanillaItems.Where(x => x.ScrapInfo != null && x.ShopInfo == null).ToList();
        }
        int randomIndex = storeSeededRandom.Next(0, vanillaItems.Count);
        return vanillaItems[randomIndex].Item;
    }

    [ServerRpc(RequireOwnership = false)]
    public void SyncGrabbableObjectScanStuffServerRpc(NetworkBehaviourReference grabbableObject, int barrelRef, int price, float borderColorR, float borderColorG, float borderColorB, float textColorR, float textColorG, float textColorB)
    {
        SyncGrabbableObjectScanStuffClientRpc(grabbableObject, barrelRef, price, borderColorR, borderColorG, borderColorB, textColorR, textColorG, textColorB);
    }

    [ClientRpc]
    public void SyncGrabbableObjectScanStuffClientRpc(NetworkBehaviourReference grabbableObjectRef, int barrelRef, int price, float borderColorR, float borderColorG, float borderColorB, float textColorR, float textColorG, float textColorB)
    {
        GrabbableObject grabbableObject = (GrabbableObject)grabbableObjectRef;
        grabbableObject.grabbable = true;
        SetupGrabbableTooltip(grabbableObject, price);
        itemsOnSale.Add(grabbableObject, price);
        MerchantBarrel barrel = existingMerchantBarrels[barrelRef];
        barrel.currentlySpawnedGrabbableObject = grabbableObject;
        barrel.textMeshPro1.text = price.ToString();
        barrel.textMeshPro2.text = price.ToString();
        ForceScanColorOnItem forceScanColorOnItem = grabbableObject.gameObject.AddComponent<ForceScanColorOnItem>();
        forceScanColorOnItem.grabbableObject = grabbableObject;
        forceScanColorOnItem.borderColor = new Color(borderColorR, borderColorG, borderColorB, 1);
        forceScanColorOnItem.textColor = new Color(textColorR, textColorG, textColorB, 1);
    }

    private void SetupGrabbableTooltip(GrabbableObject grabbableObject, int itemCost)
    {
        if (MoneyCounter.Instance == null || MoneyCounter.Instance.MoneyStored() < itemCost)
        {
            grabbableObject.customGrabTooltip = "You don't have enough money to buy this, stealing is not recommended.";
        }
        else
        {
            grabbableObject.customGrabTooltip = "You can buy this for " + itemCost + " coins.";
        }
    }
}