using UnityEngine;

namespace CodeRebirth.src.MiscScripts;

public interface ICollisionProxy
{
    virtual void OnProxyCollisionEnter(Collision collision) { }
    virtual void OnProxyCollisionStay(Collision collision) { }
    virtual void OnProxyCollisionExit(Collision collision) { }
}