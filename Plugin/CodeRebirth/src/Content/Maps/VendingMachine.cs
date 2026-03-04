using System;
using System.Collections.Generic;
using System.Linq;
using CodeRebirth.src.Content.Unlockables;
using CodeRebirth.src.MiscScripts;
using CodeRebirth.src.Util;
using Dawn.Utils;
using GameNetcodeStuff;
using Unity.Netcode;
using UnityEngine;

namespace CodeRebirth.src.Content.Maps;

public class VendingMachine : NetworkBehaviour
{
    [field: SerializeField]
    public List<SimplifiedItemWithRarityAndColor> PotentialItemsToSpawn { get; private set; } = new();

    [field: SerializeField]
    public AudioSource AudioSource { get; private set; }
    [field: SerializeField]
    public AudioClip PurchaseItemSound { get; private set; }
    [field: SerializeField]
    public AudioClip JamSound { get; private set; }

    [field: SerializeField]
    public Animator Animator { get; private set; }

    [field: SerializeField]
    public InteractTrigger SlotTrigger { get; private set; }
    [field: SerializeField]
    public Transform StartSpawnPosition { get; private set; }
    [field: SerializeField]
    public Transform EndSpawnPosition { get; private set; }
    [field: SerializeField]
    public int PayPrice { get; private set; }
    [field: SerializeField]
    public AnimationCurve JammingCurve { get; private set; } = AnimationCurve.Linear(0, 0, 1, 1);

    private List<SimplifiedRealItemWithRarityAndColor> possibleItemsToSpawn = new();
    private SimplifiedRealItemWithRarityAndColor currentItemToSpawn;
    private float jammingProgress = 0f;

    private static readonly int JammedAnimationHash = Animator.StringToHash("jammed"); // Bool
    private static readonly int UseAnimationHash = Animator.StringToHash("use"); // Trigger

    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();
        CoinDisplayUI.PointsOfInterest.Add(this.transform);
        foreach (SimplifiedItemWithRarityAndColor itemWithRarityAndColor in PotentialItemsToSpawn)
        {
            if (itemWithRarityAndColor.itemName.Equals("vanilla", StringComparison.OrdinalIgnoreCase))
            {
                possibleItemsToSpawn.Add(new SimplifiedRealItemWithRarityAndColor(null, itemWithRarityAndColor.rarity, itemWithRarityAndColor.borderColor, itemWithRarityAndColor.textColor));
                continue;
            }
            string itemName = itemWithRarityAndColor.itemName.ToLowerInvariant().Trim();
            itemWithRarityAndColor.itemName = itemName;
            foreach (Item item in StartOfRound.Instance.allItemsList.itemsList)
            {
                if (item.itemName.ToLowerInvariant().Trim() == itemName)
                {
                    possibleItemsToSpawn.Add(new SimplifiedRealItemWithRarityAndColor(item, itemWithRarityAndColor.rarity, itemWithRarityAndColor.borderColor, itemWithRarityAndColor.textColor));
                    break;
                }
            }
        }

        if (!IsServer)
        {
            return;
        }

        currentItemToSpawn = CRUtilities.ChooseRandomWeightedType(possibleItemsToSpawn.Select(x => (x, x.rarity)))!;
    }

    public override void OnNetworkDespawn()
    {
        base.OnNetworkDespawn();
        CoinDisplayUI.PointsOfInterest.Remove(this.transform);
    }

    public void StartSpawningAnimation(PlayerControllerB playerControllerB)
    {
        if (playerControllerB == null || !playerControllerB.IsLocalPlayer())
        {
            return;
        }

        StartSpawningAnimationServerRpc();
    }

    [ServerRpc(RequireOwnership = false)]
    private void StartSpawningAnimationServerRpc()
    {
        if (MoneyCounter.Instance == null || MoneyCounter.Instance.MoneyStored() < PayPrice)
        {
            return;
        }

        if (JammingCurve.Evaluate(jammingProgress) > UnityEngine.Random.Range(0f, 1f))
        {
            JamMachineClientRpc();
            return;
        }

        StartSpawningAnimationsClientRpc();
    }

    [ClientRpc]
    private void StartSpawningAnimationsClientRpc()
    {
        AudioSource.PlayOneShot(PurchaseItemSound);
        Animator.SetTrigger(UseAnimationHash);
    }

    public void SpawnRandomItem()
    {
        if (!IsServer)
        {
            return;
        }

        SpawnRandomItemServerRpc();
    }

    [ClientRpc]
    private void JamMachineClientRpc()
    {
        Animator.SetBool(JammedAnimationHash, true);
        AudioSource.PlayOneShot(JamSound);
        SlotTrigger.interactable = false;
    }

    [ServerRpc(RequireOwnership = false)]
    private void SpawnRandomItemServerRpc()
    {
        MoneyCounter.Instance.RemoveMoney(PayPrice);
        jammingProgress += 0.1f;
        Vector3 position = StartSpawnPosition.position + UnityEngine.Random.Range(0f, 1f) * (EndSpawnPosition.position - StartSpawnPosition.position).normalized;
        if (currentItemToSpawn.item == null)
        {
            currentItemToSpawn.item = Merchant.GetRandomVanillaItem(false);
        }
        GameObject itemGO = (GameObject)CodeRebirthUtils.Instance.SpawnScrap(currentItemToSpawn.item, position, false, true, 0);
        GrabbableObject grabbableObject = itemGO.GetComponent<GrabbableObject>();
        SyncGrabbableObjectScanStuffClientRpc(new NetworkBehaviourReference(grabbableObject), currentItemToSpawn.borderColor.r, currentItemToSpawn.borderColor.g, currentItemToSpawn.borderColor.b, currentItemToSpawn.textColor.r, currentItemToSpawn.textColor.g, currentItemToSpawn.textColor.b);
        currentItemToSpawn = CRUtilities.ChooseRandomWeightedType(possibleItemsToSpawn.Select(x => (x, x.rarity)))!;
    }

    [ClientRpc]
    private void SyncGrabbableObjectScanStuffClientRpc(NetworkBehaviourReference grabbableObjectRef, float borderColorR, float borderColorG, float borderColorB, float textColorR, float textColorG, float textColorB)
    {
        GrabbableObject grabbableObject = (GrabbableObject)grabbableObjectRef;
        grabbableObject.grabbable = true;
        ForceScanColorOnItem forceScanColorOnItem = grabbableObject.gameObject.AddComponent<ForceScanColorOnItem>();
        forceScanColorOnItem.grabbableObject = grabbableObject;
        forceScanColorOnItem.borderColor = new Color(borderColorR, borderColorG, borderColorB, 1);
        forceScanColorOnItem.textColor = new Color(textColorR, textColorG, textColorB, 1);
    }
}