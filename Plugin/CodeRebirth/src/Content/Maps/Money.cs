using System;
using System.Collections;
using CodeRebirth.src.Content.Unlockables;
using Dawn;
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
    private static readonly NamespacedKey CoinMesageKey = NamespacedKey.From("code_rebirth", "coin_ever_flicked");

    private static void DisplayCoinMessage()
    {
        bool flicked = Plugin.PersistentDataContainer.GetOrSetDefault(CoinMesageKey, false);
        if (!flicked)
        {
            Plugin.PersistentDataContainer.Set(CoinMesageKey, true);
            HUDManager.Instance.DisplayTip(new HUDDisplayTip("Error - TP Failed", "Denomination Analyser not located.\nPlease purchase one from the ship terminal to store currency.", HUDDisplayTip.AlertType.Hint));
        }
    }

    public void Awake()
    {
        _value = (int)_valueRange.GetRandomInRange(new System.Random(StartOfRound.Instance.randomMapSeed + _coinsSpawned));
        _coinsSpawned++;
    }

    public override void ItemActivate(bool used, bool buttonDown = true)
    {
        base.ItemActivate(used, buttonDown);
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
        playerHeldBy.activatingItem = false;

        if (MoneyCounter.Instance == null)
        {
            DisplayCoinMessage();
            return;
        }

        _moneyParticles.transform.SetParent(null, true);
        _moneyParticles.Play();
        _moneySource.PlayOneShot(_collectSound);

        if (IsServer)
        {
            MoneyCounter.Instance.AddMoney(_value);
        }

        if (IsOwner)
        {
            StartCoroutine(DestroyItemAfterDelay());
        }
    }

    private IEnumerator DestroyItemAfterDelay()
    {
        yield return new WaitForSeconds(0.1f);
        if (playerHeldBy && (isHeld || isPocketed))
        {
            playerHeldBy.DestroyItemInSlotAndSync(Array.IndexOf(playerHeldBy.ItemSlots, this));
        }
        else
        {
            DespawnItemServerRpc();
        }
    }

    [ServerRpc(RequireOwnership = false)]
    public void DespawnItemServerRpc()
    {
        NetworkObject.Despawn();
    }
}