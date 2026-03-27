using UnityEngine;

namespace CodeRebirth.src.MiscScripts;

public class TriggerProxy : MonoBehaviour
{
    [field: SerializeField]
    public InterfaceReference<ITriggerProxy> Trigger { get; private set; }

    private void OnTriggerEnter(Collider other) => Trigger.Value.OnProxyTriggerEnter(other);
    private void OnTriggerStay(Collider other) => Trigger.Value.OnProxyTriggerStay(other);
    private void OnTriggerExit(Collider other) => Trigger.Value.OnProxyTriggerExit(other);
}