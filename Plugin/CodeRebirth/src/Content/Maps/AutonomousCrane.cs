using Unity.Netcode;
using UnityEngine;

namespace CodeRebirth.src.Content.Maps;
public class AutonomousCrane : NetworkBehaviour
{
    [SerializeField]
    private Animator _leverAnimator = null!;

    [SerializeField]
    private InteractTrigger _disableInteract = null!;
}