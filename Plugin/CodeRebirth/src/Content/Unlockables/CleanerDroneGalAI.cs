using System;
using GameNetcodeStuff;
using Unity.Netcode;
using Unity.Netcode.Components;
using UnityEngine;

namespace CodeRebirth.src.Content.Unlockables;
public class CleanerDroneGalAI : NetworkBehaviour
{
    public Animator animator;
    public NetworkAnimator networkAnimator;
    public InteractTrigger SwitchPoseTrigger;

    private static readonly int AnimationStageInt = Animator.StringToHash("AnimationStageInt");

    public void Start()
    {
        SwitchPoseTrigger.onInteract.AddListener(SwitchPose);
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