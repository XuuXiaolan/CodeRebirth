using System.Collections.Generic;
using Dawn.Utils;
using GameNetcodeStuff;
using UnityEngine;

namespace CodeRebirth.src.Content.Items;
public class EazyBake : GrabbableObject
{
    [field: SerializeField]
    public float DamagerTimer { get; private set; } = 0.25f;
    [field: SerializeField]
    public float HinderedMultiplier { get; private set; } = 2f;
    [field: SerializeField]
    public Animator Animator { get; private set; }

    private List<Collider> collidersInside = new();
    private float timeSinceLastDamage = 0f; 

    private static readonly int FiringHash = Animator.StringToHash("Firing"); // Boolean

    private PlayerControllerB? playerWhoToggledOn = null;
    public override void ItemActivate(bool used, bool buttonDown = true)
    {
        base.ItemActivate(used, buttonDown);
        if (isBeingUsed)
        {
            playerWhoToggledOn = playerHeldBy;
            Animator.SetBool(FiringHash, true);
        }
        else
        {
            playerWhoToggledOn = null;
            Animator.SetBool(FiringHash, false);
        }
    }

    public override void UseUpBatteries()
    {
        base.UseUpBatteries();
        Animator.SetBool(FiringHash, false);
        playerWhoToggledOn = null;
    }

    private void FixedUpdate()
    {
        collidersInside.Clear();
    }

    internal void OnFixedUpdateStay(Collider other)
    {
        if (collidersInside.Contains(other))
        {
            return;
        }

        collidersInside.Add(other);
    }

    private List<PlayerControllerB> hitPlayers = new();
    private List<EnemyAI> hitEnemies = new();
    private List<IHittable> hitObjects = new();

    public override void Update()
    {
        base.Update();
        if (timeSinceLastDamage < Time.realtimeSinceStartup)
        {
            List<PlayerControllerB> playersFromLastTime = new(hitPlayers);
            hitPlayers.Clear();
            hitEnemies.Clear();
            hitObjects.Clear();
            timeSinceLastDamage = Time.realtimeSinceStartup + DamagerTimer;
            foreach (Collider collider in collidersInside)
            {
                if (!collider.TryGetComponent(out IHittable hittable))
                {
                    continue;
                }

                if (Physics.Linecast(collider.transform.position, this.transform.position, StartOfRound.Instance.collidersAndRoomMaskAndDefault, QueryTriggerInteraction.Ignore))
                {
                    continue;
                }

                if (hittable is PlayerControllerB player)
                {
                    if (hitPlayers.Contains(player) || player == playerHeldBy)
                    {
                        continue;
                    }

                    hitPlayers.Add(player);
                }
                else if (hittable is EnemyAICollisionDetect enemyAICollisionDetect)
                {
                    if (hitEnemies.Contains(enemyAICollisionDetect.mainScript))
                    {
                        continue;
                    }

                    hitEnemies.Add(enemyAICollisionDetect.mainScript);
                }
                else
                {
                    hitObjects.Add(hittable);
                    continue;
                }
            }

            foreach (PlayerControllerB player in hitPlayers)
            {
                if (playersFromLastTime.Contains(player))
                {
                    // was here last time
                }
                else
                {
                    player.movementSpeed /= HinderedMultiplier;
                }

                if (!player.IsLocalPlayer())
                {
                    continue;
                }

                player.DamagePlayer(5, true, true, CauseOfDeath.Gunshots, 0, false);
            }

            foreach (PlayerControllerB player in playersFromLastTime)
            {
                if (hitPlayers.Contains(player))
                {
                    // still in there
                }
                else
                {
                    player.movementSpeed *= HinderedMultiplier;
                }
            }

            foreach (EnemyAI enemyAI in hitEnemies)
            {
                enemyAI.HitFromExplosion(4);
                enemyAI.HitEnemy(1, playerWhoToggledOn, false, Plugin.BURN_HIT_ID);
            }

            if (!IsServer)
            {
                return;
            }

            foreach (IHittable hittable in hitObjects)
            {
                hittable.Hit(1, -this.transform.forward, playerWhoToggledOn, false, Plugin.BURN_HIT_ID);
            }
        }
    }
}