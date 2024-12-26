using System;
using System.Collections;
using CodeRebirth.src.MiscScripts;
using GameNetcodeStuff;
using Unity.Netcode;
using Unity.Netcode.Components;
using UnityEngine;

namespace CodeRebirth.src.Content.Unlockables;
public class ACUnitGalAI : NetworkBehaviour
{
    public Animator animator = null!;
    public NetworkAnimator networkAnimator = null!;
    public Transform gunTransform = null!;
    public InteractTrigger gunTrigger = null!;
    public InteractTrigger SwitchPoseTrigger = null!;

    private static readonly int ShootingAnimation = Animator.StringToHash("Shoot"); // trigger
    private static readonly int AnimationStageInt = Animator.StringToHash("AnimationStage"); // int

    public void Start()
    {
        gunTrigger.onInteract.AddListener(ShootPlayer);
        SwitchPoseTrigger.onInteract.AddListener(SwitchPose);
    }

    private void ShootPlayer(PlayerControllerB playerInteracting)
    {
        if (playerInteracting == null || playerInteracting != GameNetworkManager.Instance.localPlayerController) return;
        ShootPlayerServerRpc(Array.IndexOf(StartOfRound.Instance.allPlayerScripts, playerInteracting));
    }

    [ServerRpc(RequireOwnership = false)]
    private void ShootPlayerServerRpc(int playerIndex)
    {
        ShootPlayerClientRpc(playerIndex);
    }

    [ClientRpc]
    private void ShootPlayerClientRpc(int playerIndex)
    {
        StartCoroutine(ShootAnimationDelay(playerIndex));
    }

    private IEnumerator ShootAnimationDelay(int playerIndex)
    {
        PlayerControllerB affectedPlayer = StartOfRound.Instance.allPlayerScripts[playerIndex];
        networkAnimator.SetTrigger(ShootingAnimation);
        Physics.Raycast(gunTransform.position, gunTransform.forward, out RaycastHit hit, 100f, StartOfRound.Instance.collidersAndRoomMask | LayerMask.GetMask("InteractableObject"), QueryTriggerInteraction.Ignore);
        Vector3 endPosition = hit.point;
        CRUtilities.CreateExplosion(endPosition, true, 50, 0, 1, 5, CauseOfDeath.Blast, affectedPlayer, null);
        yield break;
    }

    private void SwitchPose(PlayerControllerB playerInteracting)
    {
        if (playerInteracting == null || playerInteracting != GameNetworkManager.Instance.localPlayerController) return;
        IncreaseOrDecreaseStageServerRpc();
    }

    [ServerRpc(RequireOwnership = false)]
    private void IncreaseOrDecreaseStageServerRpc()
    {
        int currentStage = animator.GetInteger(AnimationStageInt);
        Plugin.ExtendedLogging("Current Stage: " + currentStage);
        if (currentStage >= 1)
        {
            animator.SetInteger(AnimationStageInt, 0);
        }
        else
        {
            animator.SetInteger(AnimationStageInt, currentStage + 1);
        }
    }
}