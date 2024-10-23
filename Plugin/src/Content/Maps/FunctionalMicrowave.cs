using System.Collections.Generic;
using GameNetcodeStuff;
using Unity.Netcode;
using UnityEngine;

namespace CodeRebirth.src.Content.Maps;
public class FunctionalMicrowave : NetworkBehaviour
{
    public float microwaveOpeningTimer = 15f;
    public float microwaveClosingTimer = 7.5f;
    public Collider mainCollider = null!;
    public float hinderedMultiplier = 1.5f;

    private float microwaveOpening = 0f;
    private float microwaveClosing = 0f;
    private bool isOpen = false;
    private List<PlayerControllerB> playersAffected = new();

    private void Update()
    {
        if (!isOpen)
        {
            microwaveOpening += Time.deltaTime;
            if (microwaveOpening >= microwaveOpeningTimer)
            {
                microwaveOpening = 0f;
                isOpen = true;
                mainCollider.enabled = true;
                // other things related to animations etc
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
                playersAffected.Clear();
                // other things related to animations etc
            }
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player") && other.TryGetComponent(out PlayerControllerB playerControllerB))
        {
            if (!playersAffected.Contains(playerControllerB))
            {
                playersAffected.Add(playerControllerB);
                playerControllerB.hinderedMultiplier *= hinderedMultiplier;
            }
        }
    }

    private void OnTriggerStay(Collider other)
    {
        if (other.CompareTag("Player") && other.TryGetComponent(out PlayerControllerB playerControllerB))
        {
            playerControllerB.DamagePlayer(5, true, false, CauseOfDeath.Burning, 0, false, default);
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Player") && other.TryGetComponent(out PlayerControllerB playerControllerB))
        {
            if (playersAffected.Contains(playerControllerB))
            {
                playersAffected.Remove(playerControllerB);
                playerControllerB.hinderedMultiplier /= hinderedMultiplier;
            }
        }
    }
}