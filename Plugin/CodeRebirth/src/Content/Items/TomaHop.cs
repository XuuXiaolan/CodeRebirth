using UnityEngine;

namespace CodeRebirth.src.Content.Items;
public class TomaHop : CRWeapon
{
    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();
        OnEnemyHit.AddListener(OnEnemyHitEvent);
    }

    public override void OnNetworkDespawn()
    {
        base.OnNetworkDespawn();
        OnEnemyHit.RemoveListener(OnEnemyHitEvent);
    }

    public void OnEnemyHitEvent(EnemyAI enemyAI)
    {
        Plugin.ExtendedLogging($"Player's previous velocity: {previousPlayerHeldBy.velocityLastFrame}");
        // todo: do more damage based on velocity
        // todo: decide whether to launch player up based on velocity.
        // just like the minecraft mace!
        // previousPlayerHeldBy.externalForceAutoFade += Vector3.up * 20f;
    }
}