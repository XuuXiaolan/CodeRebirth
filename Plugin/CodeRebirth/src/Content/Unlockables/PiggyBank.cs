using System.Linq;
using CodeRebirth.src.Content.Maps;
using GameNetcodeStuff;
using Unity.Netcode;
using Unity.Netcode.Components;
using UnityEngine;

namespace CodeRebirth.src.Content.Unlockables;
public class PiggyBank : NetworkBehaviour, IHittable
{
    public Material[] variantMaterials = [];
    public SkinnedMeshRenderer skinnedMeshRenderer = null!;
    public Animator piggyBankAnimator = null!;
    public NetworkAnimator piggyBankNetworkAnimator = null!;

    [HideInInspector] public bool broken = false;
    private int coinsStored = 0; // Somehow save and load this, lol.
    private static readonly int BreakAnimation = Animator.StringToHash("borked"); // Bool
    private static readonly int InsertCoinAnimation = Animator.StringToHash("insertCoin"); // Trigger

    public void Start()
    {
        ApplyVariantMaterial();
    }

    public void ApplyVariantMaterial()
    {
        System.Random piggyRandom = new System.Random(StartOfRound.Instance.randomMapSeed + 32);
        Material variantMaterial = variantMaterials[piggyRandom.Next(variantMaterials.Length)];
        Material[] currentMaterials = skinnedMeshRenderer.sharedMaterials;
        currentMaterials[0] = variantMaterial;
        skinnedMeshRenderer.SetMaterials(currentMaterials.ToList());
    }

    public int AddCoinsToPiggyBank(int amount)
    {
        if (broken) return 0;
        if (IsServer) piggyBankNetworkAnimator.SetTrigger(InsertCoinAnimation);
        coinsStored += amount;
        return amount;
    }

    public bool Hit(int force, Vector3 hitDirection, PlayerControllerB? playerWhoHit = null, bool playHitSFX = false, int hitID = -1)
    {
        if (broken) return false;
        if (IsServer) piggyBankAnimator.SetTrigger(BreakAnimation);
        broken = true;
        return true;
    }

    public void SpawnAllCoinsAnimEvent()
    {
        if (IsServer)
        {
            for (int i = 0; i < coinsStored; i++)
            {
                var coin = GameObject.Instantiate(MapObjectHandler.Instance.Money.MoneyPrefab, this.transform.position, this.transform.rotation, this.transform); // todo: check this parenting stuff, especially when breaking open piggy banks.
                coin.GetComponent<NetworkObject>().Spawn(true);
            }
        }
        coinsStored = 0;
    }
}