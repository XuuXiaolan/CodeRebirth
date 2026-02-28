using UnityEngine;

namespace CodeRebirth.src.Content.Items;

public class EzBakeBakingRegion : MonoBehaviour
{
    [field: SerializeField]
    public EazyBake EazyBake { get; private set; }

    public void OnTriggerStay(Collider other)
    {
        EazyBake.OnFixedUpdateStay(other);
    }
}