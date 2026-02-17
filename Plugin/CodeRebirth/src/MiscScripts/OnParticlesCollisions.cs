using System.Collections.Generic;
using CodeRebirth.src.Util;
using Dawn;
using Unity.Netcode;
using UnityEngine;

namespace CodeRebirth.src.MiscScripts;

public class OnParticlesCollision : MonoBehaviour
{
    [field: SerializeField]
    public ParticleSystem ParticleSystem { get; private set; }

    void OnParticleCollision(GameObject other)
    {
        if (!NetworkManager.Singleton.IsServer)
        {
            return;
        }

        Plugin.Logger.LogInfo("Particle hit: " + other.name);

        // Get collision details (position, normal, etc.)
        List<ParticleCollisionEvent> collisionEvents = new();
        int numCollisionEvents = ParticleSystem.GetCollisionEvents(other, collisionEvents);

        if (numCollisionEvents > 0)
        {
            CodeRebirthUtils.Instance.SpawnScrap(LethalContent.Items[CodeRebirthItemKeys.Coin].Item, collisionEvents[0].intersection + Vector3.up * 0.25f, false, true, 0);
        }
    }
}