using CodeRebirthLib.MiscScriptManagement;
using UnityEngine;

namespace CodeRebirth.src.Content.Items;

[RequireComponent(typeof(OwnerNetworkAnimator))]
public class PlushieItem : GrabbableObject
{
    [SerializeField]
    private OwnerNetworkAnimator _ownerNetworkAnimator = null!;
    [SerializeField]
    private Animator _animator = null!;

    private static readonly int DoPlushieAnimation = Animator.StringToHash("DoPlushieAnimation");

    public override void ItemActivate(bool used, bool buttonDown = true)
    {
        base.ItemActivate(used, buttonDown);
        _ownerNetworkAnimator.SetTrigger(DoPlushieAnimation);
    }
}