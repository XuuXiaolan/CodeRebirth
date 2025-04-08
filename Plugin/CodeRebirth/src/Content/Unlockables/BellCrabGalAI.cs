using System;
using GameNetcodeStuff;
using Unity.Netcode;
using Unity.Netcode.Components;
using UnityEngine;

namespace CodeRebirth.src.Content.Unlockables;
public class BellCrabGalAI : NetworkBehaviour, INoiseListener
{
    public Animator animator;
    public NetworkAnimator networkAnimator;
    public InteractTrigger SwitchPoseTrigger;

    [NonSerialized] public float boomboxTimer = 0f;
    [NonSerialized] public bool boomboxPlaying = false;
    private static readonly int AnimationStageInt = Animator.StringToHash("AnimationStageInt");
    private static readonly int isDancing = Animator.StringToHash("isDancing");

    public void Start()
    {
        SwitchPoseTrigger.onInteract.AddListener(SwitchPose);
    }

    public void Update()
    {
        BoomboxUpdate();
    }

    private void BoomboxUpdate()
    {
        if (!boomboxPlaying || !IsServer) return;

        boomboxTimer += Time.deltaTime;
        if (boomboxTimer >= 2f)
        {
            boomboxTimer = 0f;
            boomboxPlaying = false;
            animator.SetBool(isDancing, false);
        }
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
        if (currentStage >= 4)
        {
            animator.SetInteger(AnimationStageInt, 0);
        }
        else
        {
            animator.SetInteger(AnimationStageInt, currentStage + 1);
        }
    }

    public void DetectNoise(Vector3 noisePosition, float noiseLoudness, int timesPlayedInOneSpot, int noiseID)
    {
        if (noiseID == 5 && !Physics.Linecast(transform.position, noisePosition, StartOfRound.Instance.collidersAndRoomMask))
        {
            boomboxTimer = 0f;
            boomboxPlaying = true;
            animator.SetBool(isDancing, true);
        }
    }
}