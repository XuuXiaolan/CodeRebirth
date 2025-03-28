
using AntlerShed.SkinRegistry.Events;
using UnityEngine;

namespace CodeRebirthESKR.SkinRegistry.Jimothy;
public interface JimothyEventHandler : EnemyEventHandler
{
    void OnGrabMapObject(GameObject obj)
    {
    }

    void OnDropMapObject(GameObject obj)
    {
    } 
}