using System.Collections.Generic;
using System.Linq;
using CodeRebirth.src.MiscScripts;
using Dawn.Utils;
using GameNetcodeStuff;
using UnityEngine;

namespace CodeRebirth.src.Content.DevTools;

public class StoatProjectile : MonoBehaviour
{
    [field: SerializeField]
    public SpriteRenderer SpriteRenderer { get; private set; } = null!;

    [field: SerializeField]
    public List<Sprite> Sprites { get; private set; } = new();
    [field: SerializeField]
    public float DetectionRadius { get; private set; } = 1f;

    private int force = 0;
    private PlayerControllerB playerHeldBy = null!;
    public void SetupProjectile(Vector3 direction, int force, PlayerControllerB playerHeldBy)
    {
        this.transform.forward = direction;
        this.force = force;
        this.playerHeldBy = playerHeldBy;

        SpriteRenderer.sprite = Sprites[UnityEngine.Random.Range(0, Sprites.Count)];
    }

    private Collider[] collidersHit = new Collider[16];

    private List<PlayerControllerB> playersHit = new List<PlayerControllerB>();
    private List<EnemyAI> enemiesHit = new List<EnemyAI>();
    private List<IHittable> hittablesHit = new List<IHittable>();
    private List<IExplodeable> explodeablesHit = new List<IExplodeable>();

    public void Update()
    {
        this.transform.position += this.transform.forward * Time.deltaTime * 6f;

        int numHits = Physics.OverlapSphereNonAlloc(this.transform.position, DetectionRadius, collidersHit, MoreLayerMasks.PlayersAndInteractableAndEnemiesAndPropsHazardMask | StartOfRound.Instance.collidersAndRoomMaskAndDefault, QueryTriggerInteraction.Collide);
        if (numHits <= 0)
        {
            return;
        }

        playersHit.Clear();
        enemiesHit.Clear();
        hittablesHit.Clear();
        explodeablesHit.Clear();

        List<Collider> hitColliders = collidersHit.Take(numHits).ToList();
        foreach (Collider collider in hitColliders)
        {
            IExplodeable explodeable = collider.GetComponent<IExplodeable>();
            IHittable hittable = collider.GetComponent<IHittable>();

            if (explodeable != null)
            {
                if (explodeablesHit.Contains(explodeable))
                {
                    continue;
                }

                explodeablesHit.Add(explodeable);
            }

            if (hittable == null)
            {
                continue;
            }

            if (hittable is PlayerControllerB playerControllerB)
            {
                if (playersHit.Contains(playerControllerB) || playerControllerB == playerHeldBy)
                {
                    continue;
                }

                playersHit.Add(playerControllerB);
            }
            else if (hittable is EnemyAICollisionDetect enemyAICollisionDetect)
            {
                if (enemiesHit.Contains(enemyAICollisionDetect.mainScript))
                {
                    continue;
                }

                enemiesHit.Add(enemyAICollisionDetect.mainScript);
            }
            else
            {
                if (hittablesHit.Contains(hittable))
                {
                    continue;
                }

                hittablesHit.Add(hittable);
            }
        }

        foreach (PlayerControllerB playerControllerB in playersHit)
        {
            playerControllerB.DamagePlayer(force, true, true, CauseOfDeath.Burning, 0, false, default);
        }

        foreach (EnemyAI enemyAI in enemiesHit)
        {
            enemyAI.HitEnemyOnLocalClient(force, this.transform.position);
        }

        foreach (IHittable hittable in hittablesHit)
        {
            hittable.Hit(force, this.transform.position, playerHeldBy, true, -1);
        }

        foreach (IExplodeable explodeable in explodeablesHit)
        {
            explodeable.OnExplosion(force, this.transform.position, DetectionRadius);
        }

        if (playersHit.Count > 0 || enemiesHit.Count > 0 || hittablesHit.Count > 0 || explodeablesHit.Count > 0)
        {
            Destroy(this.gameObject);
        }
    }
}