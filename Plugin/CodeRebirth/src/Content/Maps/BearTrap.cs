using System;
using System.Collections;
using System.Linq;
using GameNetcodeStuff;
using Unity.Netcode;
using UnityEngine;

namespace CodeRebirth.src.Content.Maps;
public class BearTrap : CodeRebirthHazard
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
    private PlayerControllerB? playerCaught = null;
    private EnemyAI? enemyCaught = null;
    private bool isTriggered = false;
    private bool canTrigger = true;
    [NonSerialized] public bool byProduct = false;
    private Coroutine? releaseCoroutine = null;
    private static readonly int IsTrapTriggered = Animator.StringToHash("isTrapTriggered");
    private static readonly int IsTrapResetting = Animator.StringToHash("isTrapResetting");

    public override void Start()
    {
        base.Start();
        if (!IsServer || byProduct) return;
        var random = new System.Random(StartOfRound.Instance.randomMapSeed);
		Vector3 position = this.transform.position;
        if (MapObjectHandler.Instance.BearTrap == null) return;
		for (int i = 0; i < random.Next(4, 8); i++)
		{
			Vector3 vector = RoundManager.Instance.GetRandomNavMeshPositionInRadius(position, 10f) + (Vector3.up * 2);

			Physics.Raycast(vector, Vector3.down, out RaycastHit hit, 100, StartOfRound.Instance.collidersAndRoomMaskAndDefault, QueryTriggerInteraction.Ignore);

			if (hit.collider == null) continue;
            GameObject beartrap = MapObjectHandler.Instance.BearTrap.MapObjectDefinitions.GetCRMapObjectDefinitionWithObjectName("gravel")!.gameObject;
            if (hit.collider.CompareTag("Grass"))
            {
                beartrap = MapObjectHandler.Instance.BearTrap.MapObjectDefinitions.GetCRMapObjectDefinitionWithObjectName("grass")!.gameObject;;
            }
            else if (hit.collider.CompareTag("Snow"))
            {
                beartrap = MapObjectHandler.Instance.BearTrap.MapObjectDefinitions.GetCRMapObjectDefinitionWithObjectName("snow")!.gameObject;;
            }

            if (this is BoomTrap)
            {
                beartrap = MapObjectHandler.Instance.BearTrap.MapObjectDefinitions.GetCRMapObjectDefinitionWithObjectName("boom")!.gameObject;
            }
            else if (random.Next(0, 100) < 5)
            {
                beartrap = MapObjectHandler.Instance.BearTrap.MapObjectDefinitions.GetCRMapObjectDefinitionWithObjectName("boom")!.gameObject;
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
        if (isTriggered || !canTrigger) return;

        if (other.gameObject.layer == 30 && other.TryGetComponent(out WheelCollider wheel))
        {
            trapAudioSource.PlayOneShot(poppingTireSound);
            SetWheelFriction(wheel.gameObject);
        }
        else if (other.gameObject.layer == 3 && other.TryGetComponent(out PlayerControllerB player) && player == GameNetworkManager.Instance.localPlayerController)
        {
            TriggerTrapServerRpc(Array.IndexOf(StartOfRound.Instance.allPlayerScripts, player));
        }
        else if (other.gameObject.layer == 19 && other.TryGetComponent(out EnemyAI enemy))
        {
            TriggerTrap(enemy);
        }
    }

    [ServerRpc(RequireOwnership = false)]
    private void TriggerTrapServerRpc(int index)
    {
        TriggerTrapClientRpc(index);
    }

    [ClientRpc]
    private void TriggerTrapClientRpc(int index)
    {
        if (index == -1) return;
        TriggerTrap(StartOfRound.Instance.allPlayerScripts[index]);
    }

    public virtual void TriggerTrap(PlayerControllerB player)
    {
        trapAudioSource.Stop();
        trapAudioSource.clip = triggerSound;
        trapAudioSource.Play();
        isTriggered = true;
        playerCaught = player;
        playerCaught.disableMoveInput = true;
        playerCaught.DamagePlayer(25, true, false, CauseOfDeath.Crushing, 0, false, default);
        caughtPosition = playerCaught.transform.position;
        trapAnimator.SetBool(IsTrapTriggered, true);
        StartCoroutine(ResetBooleanAfterDelay(IsTrapTriggered, 0.5f));
        trapCollider.enabled = false;
    }

    public virtual void TriggerTrap(EnemyAI enemy)
    {
        trapAudioSource.Stop();
        trapAudioSource.clip = triggerSound;
        trapAudioSource.Play();
        isTriggered = true;
        enemyCaught = enemy;
        enemyCaught.HitEnemy(1, null, false, -1);
        trapAnimator.SetBool(IsTrapTriggered, true);
        StartCoroutine(ResetBooleanAfterDelay(IsTrapTriggered, 0.5f));
        trapCollider.enabled = false;

        if (releaseCoroutine != null)
        {
            StopCoroutine(releaseCoroutine);
        }
        releaseCoroutine = StartCoroutine(DelayReleasingEnemy());
    }

    private IEnumerator DelayReleasingEnemy()
    {
        yield return new WaitForSeconds(12f);
        DoReleaseTrap();
        releaseCoroutine = null;
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
        if (!isTriggered || playerCaught == null) return;

        TriggerTrap(playerCaught);
    }

    public void ReleaseTrap(PlayerControllerB player)
    {
        if (GameNetworkManager.Instance.localPlayerController != player) return;
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
        if (trapAnimator.GetBool(IsTrapTriggered))
        {
            trapAnimator.SetBool(IsTrapTriggered, false);
        }

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
}