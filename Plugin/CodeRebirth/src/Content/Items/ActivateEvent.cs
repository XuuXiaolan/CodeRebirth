using UnityEngine;
using UnityEngine.Events;

namespace CodeRebirth.src.Content.Items;

public class ActivateEvent : GrabbableObject
{
    [SerializeField]
    private UnityEvent _onActivate;

    public override void ItemActivate(bool used, bool buttonDown = true)
    {
        base.ItemActivate(used, buttonDown);
        _onActivate.Invoke();
    }
}