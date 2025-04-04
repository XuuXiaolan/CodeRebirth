using System.Collections;
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
        StartCoroutine(HandleEnemyHits(enemyScript));
    }

    public IEnumerator HandleEnemyHits(EnemyAI enemyScript)
    {
        int numHits = Physics.OverlapSphereNonAlloc(enemyScript.gameObject.transform.position, 5, cachedColliders, CodeRebirthUtils.Instance.playersAndInteractableAndEnemiesAndPropsHazardMask, QueryTriggerInteraction.Collide);
        for (int i = 0; i < numHits; i++)
        {
            yield return null;
            if (!cachedColliders[i].gameObject.TryGetComponent(out IHittable iHittable) || (iHittable is EnemyAICollisionDetect hittableEnemyScript && (hittableEnemyScript.mainScript == enemyScript || hittableEnemyScript.mainScript.isEnemyDead)) || (iHittable is PlayerControllerB player && player == previousPlayerHeldBy)) continue;
            iHittable.Hit(HitForce, previousPlayerHeldBy.transform.position, previousPlayerHeldBy, true, HitId);
        }
    }
}