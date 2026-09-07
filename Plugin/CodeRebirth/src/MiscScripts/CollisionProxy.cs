using UnityEngine;

namespace CodeRebirth.src.MiscScripts;

public class CollisionProxy : MonoBehaviour
{
    [field: SerializeField]
    public InterfaceReference<ICollisionProxy> Collision { get; private set; }

    private void OnCollisionEnter(Collision collision) => Collision.Value.OnProxyCollisionEnter(collision);
    private void OnCollisionStay(Collision collision) => Collision.Value.OnProxyCollisionStay(collision);
    private void OnCollisionExit(Collision collision) => Collision.Value.OnProxyCollisionExit(collision);
}