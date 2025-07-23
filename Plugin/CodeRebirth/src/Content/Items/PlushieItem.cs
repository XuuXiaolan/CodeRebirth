using CodeRebirth.src.Util.Extensions;
using UnityEngine;
using UnityEngine.Events;

namespace CodeRebirth.src.Content.Items;

public class PlushieItem : GrabbableObject
{
    [Header("References")]
    [SerializeField]
    private Animator _animator = null!;

    [Space(10)]
    [Header("Events")]
    [SerializeField]
    private bool _dropOnUse = false;
    [SerializeField]
    private UnityEvent _onUseEvent = null!;
    [SerializeField]
    private UnityEvent _onAnimationEnd = null!;

    [Space(10)]
    [Header("Audio")]
    [SerializeField]
    private AudioSource _audioSource = null!;
    [SerializeField]
    private AudioClip? _useSound = null;

    private static readonly int DoPlushieAnimation = Animator.StringToHash("DoPlushieAnimation");

    public override void ItemActivate(bool used, bool buttonDown = true)
    {
        base.ItemActivate(used, buttonDown);
        _animator.SetTrigger(DoPlushieAnimation);
        _onUseEvent.Invoke();
        if (_useSound != null)
        {
            _audioSource.PlayOneShot(_useSound);
        }
        if (_dropOnUse && playerHeldBy != null && playerHeldBy.IsLocalPlayer())
        {
            playerHeldBy.StartCoroutine(playerHeldBy.waitToEndOfFrameToDiscard());
        }
    }

    public void OnAnimationEndAnimEvent()
    {
        _onAnimationEnd.Invoke();
    }
}