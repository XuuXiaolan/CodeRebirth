using CodeRebirth.src.MiscScripts;
using UnityEngine;

namespace CodeRebirth.src.Content.Items;

public class JimBall : SoccerBallProp
{
    [SerializeField]
    private OwnerNetworkAnimator _ownerNetworkAnimator = null!;

    [SerializeField]
    private Animator _animator = null!;
}