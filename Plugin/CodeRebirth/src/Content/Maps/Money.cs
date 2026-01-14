using CodeRebirth.src.Content.Unlockables;
using Dawn.Utils;
using UnityEngine;

namespace CodeRebirth.src.Content.Maps;

public class Money : GrabbableObject
{
    [field: SerializeField]
    private int _value = 1;

    [field: SerializeField]
    public AudioSource _moneySource;

    [field: SerializeField]
    public AudioClip _collectSound;

    [field: SerializeField]
    public OwnerNetworkAnimator? _ownerNetworkAnimator;

    public override void ItemActivate(bool used, bool buttonDown = true)
    {
        base.ItemActivate(used, buttonDown);
        if (_ownerNetworkAnimator != null)
        {
            _ownerNetworkAnimator.SetTrigger("flip");
        }
        else
        {
            TryCollectCoin();
        }
    }

    public void TryCollectCoin()
    {
        if (MoneyCounter.Instance == null)
            return;

        _moneySource.PlayOneShot(_collectSound);

        if (!IsServer)
            return;

        MoneyCounter.Instance.AddMoney(_value);
    }
}