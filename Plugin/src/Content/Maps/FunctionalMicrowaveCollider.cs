using Unity.Netcode;
using UnityEngine;

namespace CodeRebirth.src.Content.Maps;
public class FunctionalMicrowaveCollider : NetworkBehaviour
{
    public FunctionalMicrowave mainScript;

    public void OnTriggerEnter(Collider other)
    {
        mainScript.OnColliderEnter(other);
    }

    public void OnTriggerStay(Collider other)
    {
        mainScript.OnColliderStay(other);
    }

    public void OnTriggerExit(Collider other)
    {
        mainScript.OnColliderExit(other);
    }
}