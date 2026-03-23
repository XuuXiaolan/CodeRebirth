using Unity.Netcode;
using UnityEngine;

namespace CodeRebirth.src.Content.Unlockables;
public class CruiserCharger : Charger
{
    public Animator animator = null!;
    public static readonly int isActivatedAnimation = Animator.StringToHash("IsActivated"); // bool
}