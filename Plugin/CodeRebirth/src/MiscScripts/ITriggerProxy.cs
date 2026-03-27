using UnityEngine;

namespace CodeRebirth.src.MiscScripts;

public interface ITriggerProxy
{
    virtual void OnProxyTriggerEnter(Collider other) { }
    virtual void OnProxyTriggerStay(Collider other) { }
    virtual void OnProxyTriggerExit(Collider other) { }
}