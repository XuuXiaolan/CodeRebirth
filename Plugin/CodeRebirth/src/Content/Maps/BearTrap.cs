using System;
using System.Collections;
using CodeRebirth.src.Util.Extensions;
using GameNetcodeStuff;
using Unity.Netcode;
using UnityEngine;

namespace CodeRebirth.src.Content.Maps;
public class BearTrap : NetworkBehaviour
{
    public Animator trapAnimator = null!;
    public Collider trapCollider = null!;
    public float delayBeforeReset = 3.0f;
    public InteractTrigger trapTrigger = null!;
    public AudioSource trapAudioSource = null!;
    public AudioClip resetTrapEndSound = null!;
    public AudioClip triggerSound = null!;
    public AudioClip resetTrapSound = null!;

    private Vector3 caughtPosition = Vector3.zero;
    private PlayerControllerB? playerCaught = null;
    private EnemyAI? enemyCaught = null;
    private bool isTriggered = false;
    private bool canTrigger = true;
    [NonSerialized] public bool byProduct = false;
    private Coroutine? releaseCoroutine = null;
    private static readonly int IsTrapTriggered = Animator.StringToHash("isTrapTriggered");
    private static readonly int IsTrapResetting = Animator.StringToHash("isTrapResetting");

    private void Start()
    {
        if (!IsServer || byProduct) return;
        var random = new System.Random(StartOfRound.Instance.randomMapSeed);
		Vector3 position = this.transform.position;
		for (int i = 0; i < random.NextInt(3, 6); i++)
		{
			Vector3 vector = RoundManager.Instance.GetRandomNavMeshPositionInRadius(position, 10f) + (Vector3.up * 2);

			Physics.Raycast(vector, Vector3.down, out RaycastHit hit, 100, StartOfRound.Instance.collidersAndRoomMaskAndDefault);

			if (hit.collider != null) // Check to make sure we hit something
			{
				GameObject beartrap = MapObjectHandler.Instance.BearTrap.GravelMatPrefab;
				if (hit.collider.CompareTag("Grass"))
				{
					beartrap = MapObjectHandler.Instance.BearTrap.GrassMatPrefab;
				}
				else if (hit.collider.CompareTag("Gravel"))
				{
					beartrap = MapObjectHandler.Instance.BearTrap.GravelMatPrefab;
				}
				else if (hit.collider.CompareTag("Snow"))
				{
					beartrap = MapObjectHandler.Instance.BearTrap.SnowMatPrefab;
				}

				GameObject spawnedTrap = GameObject.Instantiate(beartrap, hit.point, Quaternion.identity, RoundManager.Instance.mapPropsContainer.transform);
                spawnedTrap.GetComponent<BearTrap>().byProduct = true;
				Plugin.ExtendedLogging($"Spawning {beartrap.name} at {hit.point}");
				spawnedTrap.transform.up = hit.normal;
				spawnedTrap.GetComponent<NetworkObject>().Spawn();
                position = spawnedTrap.transform.position;
			}
		}
    }

    private void Update()
    {
        trapTrigger.interactable = isTriggered;
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

        enemyCaught.agent.speed = 0f;
    }

    private void OnTriggerEnter(Collider other)
    {
        if (isTriggered || !canTrigger) return;
        
        if (other.gameObject.layer == 3 && other.TryGetComponent(out PlayerControllerB player) && player == GameNetworkManager.Instance.localPlayerController)
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

    private void TriggerTrap(PlayerControllerB player)
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

    private void TriggerTrap(EnemyAI enemy)
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

    private void DoReleaseTrap()
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