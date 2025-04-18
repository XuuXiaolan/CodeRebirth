using CodeRebirth.src.Content.Maps;
using CodeRebirth.src.Content.Unlockables;
using CodeRebirth.src.Util;
using Unity.Netcode;
using UnityEngine;

namespace CodeRebirth.src.Content.Items;
public class Wallet : GrabbableObject
{
    [HideInInspector] public NetworkVariable<int> coinsStored = new NetworkVariable<int>(0, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);
    public AudioSource audioPlayer = null!;
    public ScanNodeProperties scanNode = null!;
    public SkinnedMeshRenderer skinnedMeshRenderer = null!;

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
        RaycastHit[] hits = Physics.RaycastAll(interactRay, playerHeldBy.grabDistance, CodeRebirthUtils.Instance.propsAndHazardMask, QueryTriggerInteraction.Collide);
        foreach (RaycastHit hit in hits)
        {
            Money? coin = hit.collider.transform.gameObject.GetComponent<Money>();
            PiggyBank? piggyBank = hit.collider.transform.gameObject.GetComponent<PiggyBank>();
            if (coin == null && piggyBank == null)
                continue;

            if (coin != null)
            {
                audioPlayer.Play();
                if (IsServer) coinsStored.Value += coin.value;
                scanNode.subText = $"Coins Stored: {coinsStored.Value}";
                if (IsServer) coin.NetworkObject.Despawn();
            }
            else if (IsServer)
            {
                int coins = piggyBank.AddCoinsToPiggyBank(coinsStored.Value);
                coinsStored.Value -= coins;
            }
        }
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