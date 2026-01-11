using UnityEngine;
using UnityEngine.Events;

namespace CodeRebirth.src.Content.Items;

public class InteractableItem : GrabbableObject
{
    [field: SerializeField]
    public UnityEvent OnItemActivate { get; private set; } = new UnityEvent();

    public override void ItemActivate(bool used, bool buttonDown = true)
    {
        base.ItemActivate(used, buttonDown);
        OnItemActivate?.Invoke();
    }
}