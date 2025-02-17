using CodeRebirth.src.Content.Maps;
using CodeRebirth.src.Content.Unlockables;
using CodeRebirth.src.Util;
using UnityEngine;

namespace CodeRebirth.src.Content.Items;
public class Wallet : GrabbableObject
{
    private int coinsStored = 0;
    public AudioSource audioPlayer = null!;
    public ScanNodeProperties scanNode = null!;
    public SkinnedMeshRenderer skinnedMeshRenderer = null!;

	public override int GetItemDataToSave()
	{
		base.GetItemDataToSave();
		return coinsStored;
	}

	public override void LoadItemSaveData(int saveData)
	{
		base.LoadItemSaveData(saveData);
		coinsStored = saveData;
	}

    private void UpdateWalletSize()
    {
        skinnedMeshRenderer.SetBlendShapeWeight(0, Mathf.Clamp(coinsStored * 20f, 0, 300));
    }

    public override void ItemActivate(bool used, bool buttonDown = true)
    {
        base.ItemActivate(used, buttonDown);
        if (playerHeldBy == null) return;
        Ray interactRay = new Ray(playerHeldBy.gameplayCamera.transform.position, playerHeldBy.gameplayCamera.transform.forward);
        RaycastHit[] hits = Physics.RaycastAll(interactRay, playerHeldBy.grabDistance, CodeRebirthUtils.Instance.propsAndHazardMask, QueryTriggerInteraction.Collide);
        foreach (RaycastHit hit in hits)
        {
            Money coin = hit.collider.transform.gameObject.GetComponent<Money>();
            PiggyBank piggyBank = hit.collider.transform.gameObject.GetComponent<PiggyBank>();
            if (coin == null && piggyBank == null) continue;
            if (coin != null)
            {
                audioPlayer.Play();
                coinsStored += coin.value;
                scanNode.subText = $"Coins Stored: {coinsStored}";
                if (IsServer) coin.NetworkObject.Despawn();
                return;
            }
            else
            {
                int coins = piggyBank.AddCoinsToPiggyBank(coinsStored);
                coinsStored -= coins;
            }
        }
    }

    public override void Update()
    {
        base.Update();
        scanNode.subText = $"Coins Stored: {coinsStored}";
        UpdateWalletSize();
    }
}