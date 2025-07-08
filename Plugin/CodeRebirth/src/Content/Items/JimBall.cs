using CodeRebirthLib.MiscScriptManagement;
using UnityEngine;

namespace CodeRebirth.src.Content.Items;
public class JimBall : SoccerBallProp
{
    [SerializeField]
    private OwnerNetworkAnimator _ownerNetworkAnimator = null!;

    [SerializeField]
    internal Animator _animator = null!;

    internal static readonly int KickingAnimation = Animator.StringToHash("kicking"); // Boolean
    internal static readonly int HeldAnimation = Animator.StringToHash("held"); // Boolean

    public override void EquipItem()
    {
        base.EquipItem();
        _animator.SetBool(HeldAnimation, true);
    }

    public override void DiscardItem()
    {
        base.DiscardItem();
        _animator.SetBool(HeldAnimation, false);
    }

    public override void PlayDropSFX()
    {
        base.PlayDropSFX();
        if (!IsOwner)
            return;

        _animator.SetBool(KickingAnimation, false);
    }
}