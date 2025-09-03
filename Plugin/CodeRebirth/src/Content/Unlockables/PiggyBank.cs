using System.Collections;
using CodeRebirth.src.Util;
using Dawn;
using GameNetcodeStuff;
using Unity.Netcode;
using Unity.Netcode.Components;
using UnityEngine;

namespace CodeRebirth.src.Content.Unlockables;

public class PiggyBank : NetworkBehaviour, IHittable
{
    public AudioSource _audioSource = null!;
    public AudioClip _breakBankSound = null!;
    public AudioClip _storeCoinSound = null!;
    public Material[] variantMaterials = [];
    public SkinnedMeshRenderer skinnedMeshRenderer = null!;
    public Animator piggyBankAnimator = null!;
    public NetworkAnimator piggyBankNetworkAnimator = null!;

    private NetworkVariable<bool> _broken = new NetworkVariable<bool>(false, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);
    private NetworkVariable<int> _coinsStored = new NetworkVariable<int>(0, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);
    private static readonly int _BreakAnimation = Animator.StringToHash("borked"); // Bool
    private static readonly int _InsertCoinAnimation = Animator.StringToHash("insertCoin"); // Trigger

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

    public IEnumerator Start()
    {
        yield return new WaitUntil(() => StartOfRound.Instance.randomMapSeed != 0);
        ApplyVariantMaterial();
    }

    public void ApplyVariantMaterial()
    {
        System.Random piggyRandom = new System.Random(StartOfRound.Instance.randomMapSeed + 32);
        Material variantMaterial = variantMaterials[piggyRandom.Next(variantMaterials.Length)];
        Material[] currentMaterials = skinnedMeshRenderer.sharedMaterials;
        currentMaterials[0] = variantMaterial;
        skinnedMeshRenderer.sharedMaterials = currentMaterials;
    }

    public int AddCoinsToPiggyBank(int amount)
    {
        if (_broken.Value)
            return 0;

        if (IsServer)
        {
            _coinsStored.Value += amount;
            piggyBankNetworkAnimator.SetTrigger(_InsertCoinAnimation);
            if (StartOfRound.Instance.inShipPhase) SaveCurrentCoins();
        }
        return amount;
    }

    public void SaveCurrentCoins()
    {
        if (!IsHost) return;
        ES3.Save<int>("coinsStoredCR", _coinsStored.Value, CodeRebirthUtils.Instance.SaveSettings);
    }

    public bool Hit(int force, Vector3 hitDirection, PlayerControllerB? playerWhoHit = null, bool playHitSFX = false, int hitID = -1)
    {
        if (_broken.Value)
            return false;

        RepairOrBreakPiggyBankServerRpc(true);
        return true;
    }

    [ServerRpc(RequireOwnership = false)]
    public void SetPiggyBankBrokenServerRpc()
    {
        piggyBankAnimator.SetBool(_BreakAnimation, true);
    }

    [ServerRpc(RequireOwnership = false)]
    public void RepairOrBreakPiggyBankServerRpc(bool broken)
    {
        piggyBankAnimator.SetBool(_BreakAnimation, broken);
        _broken.Value = broken;
    }

    public void SpawnAllCoinsAnimEvent()
    {
        _audioSource.PlayOneShot(_breakBankSound);
        if (IsServer)
        {
            for (int i = 0; i < _coinsStored.Value; i++)
            {
                var coin = GameObject.Instantiate(LethalContent.MapObjects[CodeRebirthMapObjectKeys.Money].MapObject, this.transform.position, this.transform.rotation, this.transform); // todo: check this parenting stuff, especially when breaking open piggy banks.
                coin.GetComponent<NetworkObject>().Spawn(true);
            }
            _coinsStored.Value = 0;
        }
    }

    public void StoreCoinSoundAnimEvent()
    {
        _audioSource.PlayOneShot(_storeCoinSound);
    }
}