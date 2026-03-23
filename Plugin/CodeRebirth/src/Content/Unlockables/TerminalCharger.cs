using Unity.Netcode;
using UnityEngine;

namespace CodeRebirth.src.Content.Unlockables;
public class TerminalCharger : Charger
{
    public Animator animator = null!;
    public static readonly int isOpenedAnimation = Animator.StringToHash("isOpen"); // bool
}