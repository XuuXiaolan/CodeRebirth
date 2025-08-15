using CodeRebirth.src.Content.Maps;
using CodeRebirth.src.Content.Unlockables;
using CodeRebirthLib;
using CodeRebirthLib.CRMod;
using CodeRebirthLib.Utils;
using Unity.Netcode;
using UnityEngine;

namespace CodeRebirth.src.Content.Items;
public class Wallet : GrabbableObject
{
    public AudioSource audioPlayer = null!;
    public ScanNodeProperties scanNode = null!;
    public SkinnedMeshRenderer skinnedMeshRenderer = null!;

    [HideInInspector] public NetworkVariable<int> coinsStored = new NetworkVariable<int>(0, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);
    public override int GetItemDataToSave()
    {
        base.GetItemDataToSave();
        return coinsStored.Value;
    }

    public override void LoadItemSaveData(int saveData)
    {
        base.LoadItemSaveData(saveData);
        coinsStored.Value = saveData;
    }

    private void UpdateWalletSize()
    {
        skinnedMeshRenderer.SetBlendShapeWeight(0, Mathf.Clamp(coinsStored.Value * 20f, 0, 300));
    }

    public override void ItemActivate(bool used, bool buttonDown = true)
    {
        base.ItemActivate(used, buttonDown);
        if (playerHeldBy == null)
            return;

        Ray interactRay = new(playerHeldBy.gameplayCamera.transform.position, playerHeldBy.gameplayCamera.transform.forward);
        RaycastHit[] hits = Physics.RaycastAll(interactRay, playerHeldBy.grabDistance, MoreLayerMasks.PropsAndHazardMask, QueryTriggerInteraction.Collide);
        foreach (RaycastHit hit in hits)
        {
            Money? coin = hit.collider.transform.gameObject.GetComponent<Money>();
            PiggyBank? piggyBank = hit.collider.transform.gameObject.GetComponent<PiggyBank>();
            if (coin == null && piggyBank == null)
                continue;

            if (coin != null)
            {
                if (!playerHeldBy.IsLocalPlayer())
                    continue;

                audioPlayer.Play();
                var ohapennyKey = NamespacedKey<CRMAchievementDefinition>.From("code_rebirth", "oh_a_penny");
                CRModContent.Achievements.TryTriggerAchievement(ohapennyKey);
                var walletSizeKey = NamespacedKey<CRMAchievementDefinition>.From("code_rebirth", "fat_wallet");
                CRModContent.Achievements.TryIncrementAchievement(walletSizeKey, 1f);
                AddCoinsServerRpc(new NetworkObjectReference(coin.NetworkObject), coin.value);
            }
            else if (IsServer)
            {
                int coins = piggyBank.AddCoinsToPiggyBank(coinsStored.Value);
                coinsStored.Value -= coins;
            }
        }
    }


    [ServerRpc(RequireOwnership = false)]
    public void AddCoinsServerRpc(NetworkObjectReference networkObjectReference, int amount)
    {
        coinsStored.Value += amount;
        if (networkObjectReference.TryGet(out NetworkObject netObj))
        {
            netObj.Despawn();
        }
        UpdateScanNodeClientRpc();
    }

    [ClientRpc]
    public void UpdateScanNodeClientRpc()
    {
        scanNode.subText = $"Coins Stored: {coinsStored.Value}";
    }

    [ServerRpc(RequireOwnership = false)]
    public void ResetCoinsServerRpc(int amount)
    {
        coinsStored.Value = amount;
    }

    public override void Update()
    {
        base.Update();
        scanNode.subText = $"Coins Stored: {coinsStored.Value}";
        UpdateWalletSize();
    }
}