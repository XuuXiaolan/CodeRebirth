using CodeRebirth.src.Util;
using GameNetcodeStuff;
using UnityEngine;

namespace CodeRebirth.src.Content.Items;
public class Swatter : CRWeapon
{
    private Collider[] cachedColliders = new Collider[16];
    public override void Start()
    {
        base.Start();
        OnEnemyHit.AddListener(OnEnemyHitEvent);
    }

    public void OnEnemyHitEvent(EnemyAI enemyScript)
    {
        int numHits = Physics.OverlapSphereNonAlloc(enemyScript.gameObject.transform.position, 5, cachedColliders, CodeRebirthUtils.Instance.playersAndInteractableAndEnemiesAndPropsHazardMask, QueryTriggerInteraction.Collide);
        for (int i = 0; i < numHits; i++)
        {
            if (!cachedColliders[i].gameObject.TryGetComponent(out IHittable iHittable) || (iHittable is EnemyAI hittableEnemyScript && (hittableEnemyScript == enemyScript || hittableEnemyScript.isEnemyDead)) || (iHittable is PlayerControllerB player && player == previousPlayerHeldBy)) continue;
            iHittable.Hit(HitForce, previousPlayerHeldBy.transform.position, previousPlayerHeldBy, true, HitId);
        }
    }
}