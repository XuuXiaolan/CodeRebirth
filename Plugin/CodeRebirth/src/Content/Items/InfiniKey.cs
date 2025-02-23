using UnityEngine;

namespace CodeRebirth.src.Content.Items;
public class InfiniKey : KnifeItem
{

    public void OnHit(Collider collider)
    {
        if (collider == null) return;
        if (collider.gameObject.TryGetComponent(out DoorLock doorlock) && doorlock.isLocked)
        {
            doorlock.UnlockDoorServerRpc();
            return;
        }
        if (collider.gameObject.TryGetComponent(out Pickable pickable) && pickable.IsLocked)
        {
            pickable.Unlock();
            return;
        }
    }
}