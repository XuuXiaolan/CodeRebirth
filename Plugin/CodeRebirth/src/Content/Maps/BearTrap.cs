using System;
using System.Collections;
using Dawn;
using Dawn.Utils;
using GameNetcodeStuff;
using Unity.AI.Navigation;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.AI;

namespace CodeRebirth.src.Content.Maps;

public class BearTrap : CodeRebirthHazard, IHittable
{
    public Animator trapAnimator = null!;
    public Collider trapCollider = null!;
    public float delayBeforeReset = 3.0f;
    public InteractTrigger trapTrigger = null!;
    public AudioSource trapAudioSource = null!;
    public AudioClip resetTrapEndSound = null!;
    public AudioClip triggerSound = null!;
    public AudioClip resetTrapSound = null!;
    public AudioClip poppingTireSound = null!;

    private Vector3 caughtPosition = Vector3.zero;
    internal PlayerControllerB? playerCaught = null;
    private EnemyAI? enemyCaught = null;
    private bool isTriggered = false;
    private bool canTrigger = true;
    [NonSerialized] public bool byProduct = false;
    private static readonly int IsTrapTriggered = Animator.StringToHash("isTrapTriggered");
    private static readonly int IsTrapResetting = Animator.StringToHash("isTrapResetting");

    public override void Start()
    {
        base.Start();
        if (!IsServer)
            return;

        float newTrapTime = UnityEngine.Random.Range(trapTrigger.timeToHold - 1.5f, trapTrigger.timeToHold + 0.5f);
        SyncRandomResetTrapTimeClientRpc(newTrapTime);
        if (byProduct)
            return;

        Vector3 position = this.transform.position;
        for (int i = 0; i < UnityEngine.Random.Range(4, 8) - (this is BoomTrap ? 3 : 0); i++)
        {
            Vector3 vector = RoundManager.Instance.GetRandomNavMeshPositionInRadius(position, 10f) + (Vector3.up * 2);

            Physics.Raycast(vector, Vector3.down, out RaycastHit hit, 100, StartOfRound.Instance.collidersAndRoomMaskAndDefault, QueryTriggerInteraction.Ignore);

            if (hit.collider == null)
                continue;

            var mapObjectInfo = LethalContent.MapObjects[CodeRebirthMapObjectKeys.GravelBearTrap];
            GameObject beartrap = mapObjectInfo.MapObject;

            if (hit.collider.CompareTag("Grass"))
            {
                mapObjectInfo = LethalContent.MapObjects[CodeRebirthMapObjectKeys.GrassBearTrap];
                beartrap = mapObjectInfo.MapObject;
            }
            else if (hit.collider.CompareTag("Snow"))
            {
                mapObjectInfo = LethalContent.MapObjects[CodeRebirthMapObjectKeys.SnowBearTrap];
                beartrap = mapObjectInfo.MapObject;
            }

            if (this is BoomTrap)
            {
                mapObjectInfo = LethalContent.MapObjects[CodeRebirthMapObjectKeys.BoomTrap];
                beartrap = mapObjectInfo.MapObject;
            }
            else if (UnityEngine.Random.Range(0, 1000) < 5)
            {
                mapObjectInfo = LethalContent.MapObjects[CodeRebirthMapObjectKeys.BoomTrap];
                beartrap = mapObjectInfo.MapObject;
            }
            GameObject spawnedTrap = GameObject.Instantiate(beartrap, hit.point, Quaternion.identity, RoundManager.Instance.mapPropsContainer.transform);
            spawnedTrap.GetComponent<BearTrap>().byProduct = true;
            Plugin.ExtendedLogging($"Spawning {beartrap.name} at {hit.point}");
            spawnedTrap.transform.up = hit.normal;
            spawnedTrap.GetComponent<NetworkObject>().Spawn(true);
            position = spawnedTrap.transform.position;
        }
        this.NetworkObject.Despawn(true);
    }

    private void Update()
    {
        trapTrigger.interactable = isTriggered;
        UpdateAudio();
        if (playerCaught != null)
        {
            float distanceToPlayer = Vector3.Distance(playerCaught.transform.position, this.transform.position);
            if (distanceToPlayer > 15)
            {
                DoReleaseTrap();
                return;
            }
            playerCaught.transform.position = Vector3.Lerp(playerCaught.transform.position, caughtPosition, 5f * Time.deltaTime);
        }
        if (enemyCaught == null) return;

        enemyCaught.agent.velocity = Vector3.zero;
    }

    public void SetWheelFriction(GameObject wheelHitGameObject)
    {
        if (!Plugin.ModConfig.ConfigBearTrapsPopTires.Value) return;
        WheelCollider wheelCollider = wheelHitGameObject.GetComponent<WheelCollider>();

        WheelFrictionCurve sidewaysFriction = wheelCollider.sidewaysFriction;
        sidewaysFriction.stiffness = 0.1f;  // Decrease stiffness to simulate reduced grip
        sidewaysFriction.extremumValue = 0.1f;  // Very little grip at the extremum point
        sidewaysFriction.asymptoteValue = 0.05f;  // Even less grip at the asymptote point
        sidewaysFriction.extremumSlip = 0.2f;  // The point where grip drops sharply (this could vary based on how flat the tire is)
        sidewaysFriction.asymptoteSlip = 0.5f;  // Adjust based on how you want the tire to behave at high slip

        WheelFrictionCurve forwardFriction = wheelCollider.forwardFriction;
        forwardFriction.stiffness = 0.1f;  // Similar to sideways friction, reduce stiffness
        forwardFriction.extremumValue = 0.1f;  // Reduce friction at extremum
        forwardFriction.asymptoteValue = 0.05f;  // Further reduce friction at asymptote
        forwardFriction.extremumSlip = 0.2f;  // Lower slip for the point where the tire loses traction
        forwardFriction.asymptoteSlip = 0.5f;  // Lower this for when traction is very reduced

        wheelCollider.sidewaysFriction = sidewaysFriction;
        wheelCollider.forwardFriction = forwardFriction;
    }

    private void UpdateAudio()
    {
        trapAudioSource.volume = Plugin.ModConfig.ConfigBearTrapVolume.Value;
    }

    private void OnTriggerEnter(Collider other)
    {
        if (isTriggered || !canTrigger)
        {
            return;
        }

        if (other.gameObject.layer == 30 && other.TryGetComponent(out WheelCollider wheel))
        {
            trapAudioSource.PlayOneShot(poppingTireSound);
            SetWheelFriction(wheel.gameObject);
        }
        else if (other.gameObject.layer == 3 && other.TryGetComponent(out PlayerControllerB player) && player.IsLocalPlayer())
        {
            TriggerTrapServerRpc(player);
        }
        else if (other.gameObject.layer == 19 && other.TryGetComponent(out EnemyAICollisionDetect enemyAICollisionDetect))
        {
            if (enemyAICollisionDetect.mainScript.enemyType.EnemySize == EnemySize.Giant)
            {
                return;
            }

            TriggerTrap(enemyAICollisionDetect.mainScript);
        }
    }

    [ServerRpc(RequireOwnership = false)]
    private void TriggerTrapServerRpc(PlayerControllerReference playerControllerReference)
    {
        TriggerTrapClientRpc(playerControllerReference);
    }

    [ClientRpc]
    private void TriggerTrapClientRpc(PlayerControllerReference playerControllerReference)
    {
        TriggerTrap(playerControllerReference);
    }

    public virtual void TriggerTrap(PlayerControllerB player)
    {
        playerCaught = player;
        playerCaught.disableMoveInput = true;
        playerCaught.DamagePlayer(25, true, true, CauseOfDeath.Crushing, 0, false, default);
        caughtPosition = playerCaught.transform.position;

        TriggerTrap();
    }

    public virtual void TriggerTrap()
    {
        trapAudioSource.Stop();
        trapAudioSource.clip = triggerSound;
        trapAudioSource.Play();
        isTriggered = true;
        trapAnimator.SetBool(IsTrapTriggered, true);
        StartCoroutine(ResetBooleanAfterDelay(IsTrapTriggered, 0.5f));
        trapCollider.enabled = false;
    }

    public virtual void TriggerTrap(EnemyAI enemy)
    {
        enemyCaught = enemy;
        enemyCaught.HitEnemy(0, null, false, -1);

        TriggerTrap();

        StartCoroutine(DelayReleasingEnemy(enemy));
    }

    private IEnumerator DelayReleasingEnemy(EnemyAI enemy)
    {
        yield return new WaitForSeconds(10f * (enemy.enemyType.EnemySize == EnemySize.Medium ? 0.5f : 1f) * enemy.enemyType.stunTimeMultiplier);
        DoReleaseTrap();
    }

    public void ReleaseTrapEarly()
    {
        Plugin.ExtendedLogging("release trap early");
        DoReleaseTrapEarlyServerRpc();
    }

    [ServerRpc(RequireOwnership = false)]
    private void DoReleaseTrapEarlyServerRpc()
    {
        DoReleaseTrapEarlyClientRpc();
    }

    [ClientRpc]
    private void DoReleaseTrapEarlyClientRpc()
    {
        trapAnimator.SetBool(IsTrapResetting, true);
        trapAudioSource.Stop();
        trapAudioSource.clip = resetTrapSound;
        trapAudioSource.Play();
        StartCoroutine(ResetBooleanAfterDelay(IsTrapResetting, 0.5f));
    }

    public void OnCancelReleaseTrap()
    {
        Plugin.ExtendedLogging("Canceling trap release");
        DoOnCancelReleaseTrapServerRpc();
    }

    [ServerRpc(RequireOwnership = false)]
    public void DoOnCancelReleaseTrapServerRpc()
    {
        DoOnCancelReleaseTrapClientRpc();
    }

    [ClientRpc]
    public void DoOnCancelReleaseTrapClientRpc()
    {
        if (!isTriggered || playerCaught == null)
        {
            return;
        }

        TriggerTrap(playerCaught);
    }

    public void ReleaseTrap(PlayerControllerB player)
    {
        if (!player.IsLocalPlayer())
        {
            return;
        }

        Plugin.ExtendedLogging("release trap");
        DoReleaseTrapServerRpc();
    }

    [ServerRpc(RequireOwnership = false)]
    private void DoReleaseTrapServerRpc()
    {
        DoReleaseTrapClientRpc();
    }

    [ClientRpc]
    public void DoReleaseTrapClientRpc()
    {
        DoReleaseTrap();
    }

    public void DoReleaseTrap()
    {
        trapAudioSource.Stop();
        trapAudioSource.clip = resetTrapEndSound;
        trapAudioSource.Play();

        trapAnimator.SetBool(IsTrapResetting, true);
        trapCollider.enabled = true;

        if (playerCaught != null)
        {
            playerCaught.disableMoveInput = false;
            playerCaught = null;
        }

        if (enemyCaught != null)
        {
            enemyCaught = null;
        }

        isTriggered = false;
        StartCoroutine(DelayForReuse());

        // Reset `isTrapTriggered` if it hasn't been reset properly before
        trapAnimator.SetBool(IsTrapTriggered, false);

        StartCoroutine(ResetBooleanAfterDelay(IsTrapResetting, 0.5f));
    }

    private IEnumerator DelayForReuse()
    {
        canTrigger = false;
        yield return new WaitForSeconds(delayBeforeReset);
        canTrigger = true;
    }

    private IEnumerator ResetBooleanAfterDelay(int parameterHash, float delay)
    {
        yield return new WaitForSeconds(delay);
        trapAnimator.SetBool(parameterHash, false);
    }

    [ClientRpc]
    private void SyncRandomResetTrapTimeClientRpc(float resetTime)
    {
        trapTrigger.timeToHold = resetTime;
    }

    public bool Hit(int force, Vector3 hitDirection, PlayerControllerB? playerWhoHit = null, bool playHitSFX = false, int hitID = -1)
    {
        return true;
    }
}