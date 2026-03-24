using UnityEngine;

namespace CodeRebirth.src.Content.Items;

public class MouseTrap : GrabbableObject
{
    private bool _armed = false;
    public void OnTriggerEnter(Collider other)
    {
        // damage enemy or player if _armed
        if (!_armed)
        {
            return;
        }

        _armed = false;
    }

    public override void ItemActivate(bool used, bool buttonDown = true)
    {
        base.ItemActivate(used, buttonDown);
        _armed = true;
    }
}