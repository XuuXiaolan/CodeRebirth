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
        // so fall value lower than -10 means the player is falling a sufficient speed,
        if (previousPlayerHeldBy.fallValue > 0)
            return;

        int newFallValue = (int)previousPlayerHeldBy.fallValue * -1;
        previousPlayerHeldBy.ResetFallGravity();
        previousPlayerHeldBy.externalForces = Vector3.up * newFallValue;
        previousPlayerHeldBy.externalForceAutoFade += Vector3.up * newFallValue;
        enemyAI.HitEnemyOnLocalClient(newFallValue / 10, transform.position, previousPlayerHeldBy, true, -1);
        // todo: press R and send the user up in the air.
        // todo: when not hitting anything whilst landing from a fall, take reduced fall damage.
        // todo: if hit an enemy, take no fall damage.
    }
}