using System.Collections.Generic;
using GameNetcodeStuff;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.AI;

namespace CodeRebirth.src.Content.Maps;
public class FunctionalMicrowave : NetworkBehaviour
{
    public float microwaveOpeningTimer = 15f;
    public float microwaveClosingTimer = 7.5f;
    public Collider mainCollider = null!;
    public float hinderedMultiplier = 1.5f;
    public int damageAmount = 3;
    public float damageTimer = 0.1f;
    public Animator animator = null!;
    public NavMeshAgent agent = null!;
    public float Speed = 3f;
    public float TurnSpeed = 10f;

    private float microwaveOpening = 0f;
    private float microwaveClosing = 0f;
    private bool isOpen = true;
    private float damageTimerDecrease = 0f;
    private Vector3 newDestination = default;
    private List<PlayerControllerB> playersAffected = new();

    private void Start()
    {
        microwaveClosing = microwaveClosingTimer;
        microwaveOpening = microwaveOpeningTimer;
        animator.SetBool("isActivated", isOpen);
        agent.speed = Speed;
        agent.acceleration = 5f;
        agent.angularSpeed = TurnSpeed;
        if (!IsServer) return;
        newDestination = RoundManager.Instance.insideAINodes[Random.Range(0, RoundManager.Instance.insideAINodes.Length)].transform.position;
    }

    private void Update()
    {
        damageTimerDecrease -= Time.deltaTime;
        if (!isOpen)
        {
            microwaveOpening += Time.deltaTime;
            if (microwaveOpening >= microwaveOpeningTimer)
            {
                microwaveOpening = 0f;
                isOpen = true;
                mainCollider.enabled = true;
                animator.SetBool("isActivated", isOpen);
            }
        }
        else
        {
            microwaveClosing += Time.deltaTime;
            if (microwaveClosing >= microwaveClosingTimer)
            {
                microwaveClosing = 0f;
                isOpen = false;
                mainCollider.enabled = false;
                foreach (PlayerControllerB player in playersAffected)
                {
                    player.movementSpeed *= hinderedMultiplier;
                }
                playersAffected.Clear();
                animator.SetBool("isActivated", isOpen);
            }
        }

        if (!IsServer) return;
        agent.SetDestination(newDestination);
        if (Vector3.Distance(transform.position, newDestination) < 1.5f) newDestination = RoundManager.Instance.insideAINodes[Random.Range(0, RoundManager.Instance.insideAINodes.Length)].transform.position;
    }

    public void OnColliderEnter(Collider other)
    {
        if (other.CompareTag("Player") && other.TryGetComponent(out PlayerControllerB playerControllerB))
        {
            if (!playersAffected.Contains(playerControllerB))
            {
                playersAffected.Add(playerControllerB);
                playerControllerB.movementSpeed /= hinderedMultiplier;
            }
        }
    }

    public void OnColliderStay(Collider other)
    {
        if (other.CompareTag("Player") && other.TryGetComponent(out PlayerControllerB playerControllerB))
        {
            if (damageTimerDecrease <= 0f)
            {
                damageTimerDecrease = damageTimer;
                playerControllerB.DamagePlayer(damageAmount, true, false, CauseOfDeath.Burning, 0, false, default);
            }
        }
    }

    public void OnColliderExit(Collider other)
    {
        if (other.CompareTag("Player") && other.TryGetComponent(out PlayerControllerB playerControllerB))
        {
            if (playersAffected.Contains(playerControllerB))
            {
                playersAffected.Remove(playerControllerB);
                playerControllerB.movementSpeed *= hinderedMultiplier;
            }
        }
    }
}