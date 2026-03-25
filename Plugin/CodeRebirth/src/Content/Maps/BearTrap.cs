using System.Collections;
using System.Collections.Generic;
using Dawn;
using Dawn.Utils;
using Dusk;
using GameNetcodeStuff;
using Unity.Netcode;
using UnityEngine;

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

    private float damagePlayerTimer = 0f;
    private Vector3 caughtPosition = Vector3.zero;
    internal PlayerControllerB? playerCaught = null;
    private EnemyAI? enemyCaught = null;
    private bool isTriggered = false;
    private bool canTrigger = true;
    internal bool byProduct = false;
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
            damagePlayerTimer -= Time.deltaTime;
            float distanceToPlayer = Vector3.Distance(playerCaught.transform.position, this.transform.position);
            if (distanceToPlayer > 15)
            {
                DoReleaseTrap();
                return;
            }
            playerCaught.transform.position = Vector3.Lerp(playerCaught.transform.position, caughtPosition, 5f * Time.deltaTime);
        }

        if (enemyCaught == null)
        {
            return;
        }

        enemyCaught.agent.velocity = Vector3.zero;
    }

    public void SetWheelFriction(BearTrapWheelProxy bearTrapWheelProxy)
    {
        if (!Plugin.ModConfig.ConfigBearTrapsPopTires.Value)
        {
            return;
        }

        bearTrapWheelProxy.PunctureWheel();
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

        if (other.gameObject.layer == 30 && other.TryGetComponent(out BearTrapWheelProxy wheelProxy))
        {
            trapAudioSource.PlayOneShot(poppingTireSound);
            SetWheelFriction(wheelProxy);
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

    [ServerRpc(RequireOwnership = false)]
    private void TriggerTrapServerRpc()
    {
        TriggerTrapClientRpc();
    }

    [ClientRpc]
    private void TriggerTrapClientRpc()
    {
        TriggerTrap();
    }

    public virtual void TriggerTrap(PlayerControllerB player)
    {
        playerCaught = player;
        playerCaught.disableMoveInput = true;
        if (damagePlayerTimer <= 0f)
        {
            damagePlayerTimer = 0.5f;
            playerCaught.DamagePlayer(25, true, true, CauseOfDeath.Crushing, 0, false, default);
        }
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

        StartCoroutine(DelayReleasingTrap(10f * (enemy.enemyType.EnemySize == EnemySize.Medium ? 0.5f : 1f) * enemy.enemyType.stunTimeMultiplier));
    }

    [ServerRpc(RequireOwnership = false)]
    private void DelayReleasingTrapServerRpc(float timer)
    {
        DelayReleasingTrapClientRpc(timer);
    }

    [ClientRpc]
    private void DelayReleasingTrapClientRpc(float timer)
    {
        StartCoroutine(DelayReleasingTrap(timer));
    }

    private IEnumerator DelayReleasingTrap(float timer)
    {
        trapTrigger.gameObject.SetActive(false);
        yield return new WaitForSeconds(timer);

        DoReleaseTrapEarly();
        yield return new WaitForSeconds(trapTrigger.timeToHold);
        trapTrigger.gameObject.SetActive(true);
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
        DoReleaseTrapEarly();
    }

    public void DoReleaseTrapEarly()
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
        if (isTriggered)
        {
            return false;
        }

        TriggerTrapServerRpc();

        if (this is not BoomTrap)
        {
            DelayReleasingTrapServerRpc(60f);
        }

        return true;
    }
}