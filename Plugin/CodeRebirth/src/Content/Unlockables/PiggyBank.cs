using System.Collections;
using System.Linq;
using CodeRebirth.src.Content.Maps;
using CodeRebirth.src.Util;
using CodeRebirthLib.ContentManagement.Items;
using CodeRebirthLib.ContentManagement.MapObjects;
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
    private NetworkVariable<int> coinsStored = new NetworkVariable<int>(0, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);
    private static readonly int BreakAnimation = Animator.StringToHash("borked"); // Bool
    private static readonly int InsertCoinAnimation = Animator.StringToHash("insertCoin"); // Trigger

    public static PiggyBank? Instance = null;
    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();
        Instance = this;
        StartCoroutine(LoadPiggyBankData());
    }

    public IEnumerator LoadPiggyBankData()
    {
        yield return new WaitUntil(() => CodeRebirthUtils.Instance != null);
        int _coinsStored = ES3.Load("coinsStoredCR", 0, CodeRebirthUtils.Instance.SaveSettings);
        AddCoinsToPiggyBank(_coinsStored);
    }

    public override void OnNetworkDespawn()
    {
        base.OnNetworkDespawn();
        Instance = null;
    }

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
        if (IsServer)
        {
            coinsStored.Value += amount;
            piggyBankNetworkAnimator.SetTrigger(InsertCoinAnimation);
            if (StartOfRound.Instance.inShipPhase) SaveCurrentCoins();
        }
        return amount;
    }

    public void SaveCurrentCoins()
    {
        if (!IsHost) return;
        ES3.Save<int>("coinsStoredCR", coinsStored.Value, CodeRebirthUtils.Instance.SaveSettings);
    }

    public bool Hit(int force, Vector3 hitDirection, PlayerControllerB? playerWhoHit = null, bool playHitSFX = false, int hitID = -1)
    {
        if (broken) return false;
        if (IsServer) piggyBankAnimator.SetBool(BreakAnimation, true);
        broken = true;
        return true;
    }

    [ServerRpc(RequireOwnership = false)]
    public void RepairPiggyBankServerRpc()
    {
        piggyBankAnimator.SetBool(BreakAnimation, false);
        RepairPiggyBankClientRpc();
    }

    [ClientRpc]
    private void RepairPiggyBankClientRpc()
    {
        broken = false;
    }

    public void SpawnAllCoinsAnimEvent()
    {
        if (IsServer)
        {
            if (!Plugin.Mod.MapObjectRegistry().TryGetFromMapObjectName("Money", out CRMapObjectDefinition? moneyMapObjectDefinition))
                return;

            for (int i = 0; i < coinsStored.Value; i++)
            {
                var coin = GameObject.Instantiate(moneyMapObjectDefinition.GameObject, this.transform.position, this.transform.rotation, this.transform); // todo: check this parenting stuff, especially when breaking open piggy banks.
                coin.GetComponent<NetworkObject>().Spawn(true);
            }
            coinsStored.Value = 0;
        }
    }
}