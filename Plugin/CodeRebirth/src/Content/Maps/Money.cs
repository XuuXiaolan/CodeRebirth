using System;
using CodeRebirth.src.Content.Unlockables;
using Dawn.Utils;
using Unity.Netcode;
using UnityEngine;

namespace CodeRebirth.src.Content.Maps;

public class Money : GrabbableObject
{
    [SerializeField]
    private BoundedRange _valueRange = new(1, 1);

    [SerializeField]
    private ParticleSystem _moneyParticles;

    [SerializeField]
    private AudioSource _moneySource;

    [SerializeField]
    private AudioClip _collectSound;

    [SerializeField]
    private OwnerNetworkAnimator? _ownerNetworkAnimator;

    private int _value;
    private static int _coinsSpawned = 0;
    private static readonly int FlipHash = Animator.StringToHash("flip"); // Trigger

    public void Awake()
    {
        _value = (int)_valueRange.GetRandomInRange(new System.Random(StartOfRound.Instance.randomMapSeed + _coinsSpawned));
        _coinsSpawned++;
    }

    public override void ItemActivate(bool used, bool buttonDown = true)
    {
        base.ItemActivate(used, buttonDown);
        this.itemProperties.twoHanded = true;
        playerHeldBy.activatingItem = true;
        if (_ownerNetworkAnimator != null)
        {
            if (!IsOwner)
                return;

            _ownerNetworkAnimator.SetTrigger(FlipHash);
        }
        else
        {
            TryCollectCoin();
        }
    }

    public void TryCollectCoin()
    {
        this.itemProperties.twoHanded = false;
        playerHeldBy.activatingItem = false;

        if (MoneyCounter.Instance == null)
            return;

        _moneyParticles.transform.SetParent(null, true);
        _moneyParticles.Play();
        _moneySource.PlayOneShot(_collectSound);

        if (IsServer)
        {
            MoneyCounter.Instance.AddMoney(_value);
        }

        if (IsOwner)
        {
            if (playerHeldBy && (isHeld || isPocketed))
            {
                playerHeldBy.DestroyItemInSlotAndSync(Array.IndexOf(playerHeldBy.ItemSlots, this));
            }
            else
            {
                DespawnItemServerRpc();
            }
        }
    }

    [ServerRpc(RequireOwnership = false)]
    public void DespawnItemServerRpc()
    {
        NetworkObject.Despawn();
    }
}