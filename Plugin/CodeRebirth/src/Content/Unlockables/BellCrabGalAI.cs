using System;
using CodeRebirth.src.MiscScripts;
using CodeRebirth.src.Util.Extensions;
using GameNetcodeStuff;
using Unity.Netcode;
using Unity.Netcode.Components;
using UnityEngine;

namespace CodeRebirth.src.Content.Unlockables;
public class BellCrabGalAI : NetworkBehaviour
{
    public CRNoiseListener _BellCrabGalNoiseListener = null!; // todo implement this
    public Animator animator;
    public NetworkAnimator networkAnimator;
    public InteractTrigger SwitchPoseTrigger;

    [NonSerialized] public float boomboxTimer = 0f;
    [NonSerialized] public bool boomboxPlaying = false;
    private static readonly int AnimationStageInt = Animator.StringToHash("AnimationStageInt");
    private static readonly int isDancing = Animator.StringToHash("isDancing");

    public void Start()
    {
        _BellCrabGalNoiseListener._onNoiseDetected.AddListener(OnNoiseDetected);
        SwitchPoseTrigger.onInteract.AddListener(SwitchPose);
    }

    public void Update()
    {
        BoomboxUpdate();
    }

    private void BoomboxUpdate()
    {
        if (!boomboxPlaying || !IsServer)
            return;

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
        if (playerInteracting == null || !playerInteracting.IsLocalPlayer()) return;
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

    public void OnNoiseDetected(NoiseParams noiseParams)
    {
        if (!IsServer)
            return;

        if (noiseParams.noiseID != 5 || Physics.Linecast(transform.position, noiseParams.noisePosition, StartOfRound.Instance.collidersAndRoomMaskAndDefault, QueryTriggerInteraction.Ignore))
            return;

        boomboxTimer = 0f;
        boomboxPlaying = true;
        animator.SetBool(isDancing, true);
    }
}