using System;
using System.Collections;
using GameNetcodeStuff;
using Unity.Netcode;
using Unity.Netcode.Components;
using UnityEngine;

namespace CodeRebirth.src.Content.Unlockables;
public class BearTrapGalAI : NetworkBehaviour
{
    public Animator animator = null!;
    public Transform biteTransform = null!;
    public NetworkAnimator networkAnimator = null!;
    public InteractTrigger boobaTrigger = null!;
    public InteractTrigger SwitchPoseTrigger = null!;

    private static readonly int BitingAnimation = Animator.StringToHash("Biting"); // bool
    private static readonly int AnimationStageInt = Animator.StringToHash("StageAnimation"); // int

    public void Start()
    {
        boobaTrigger.onInteract.AddListener(EatPlayer);
        SwitchPoseTrigger.onInteract.AddListener(SwitchPose);
    }

    private void EatPlayer(PlayerControllerB playerInteracting)
    {
        if (playerInteracting == null || playerInteracting != GameNetworkManager.Instance.localPlayerController) return;
        BitePlayerServerRpc(Array.IndexOf(StartOfRound.Instance.allPlayerScripts, playerInteracting));
    }

    [ServerRpc(RequireOwnership = false)]
    private void BitePlayerServerRpc(int playerIndex)
    {
        BitePlayerClientRpc(playerIndex);
    }

    [ClientRpc]
    private void BitePlayerClientRpc(int playerIndex)
    {
        StartCoroutine(BiteAnimationDelay(playerIndex));
    }

    private IEnumerator BiteAnimationDelay(int playerIndex)
    {
        PlayerControllerB affectedPlayer = StartOfRound.Instance.allPlayerScripts[playerIndex];
        affectedPlayer.transform.position = biteTransform.position;
        affectedPlayer.disableMoveInput = true;
        affectedPlayer.disableInteract = true;
        animator.SetBool(BitingAnimation, true);
        yield return new WaitForSeconds(3f);
        animator.SetBool(BitingAnimation, false);
        yield return new WaitForSeconds(1f);
        affectedPlayer.disableMoveInput = false;
        affectedPlayer.disableInteract = false;
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
        Plugin.ExtendedLogging("Current Stage: " + currentStage, (int)Logging_Level.High);
        if (currentStage >= 2)
        {
            animator.SetInteger(AnimationStageInt, 0);
        }
        else
        {
            animator.SetInteger(AnimationStageInt, currentStage + 1);
        }
    }
}