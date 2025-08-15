using System;
using System.Linq;
using CodeRebirthLib.Utils;
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
    public InteractTrigger smokeTrigger;
    public Transform smokeTransform;

    private static readonly int AnimationStageInt = Animator.StringToHash("AnimationStageInt");

    public void Start()
    {
        SwitchPoseTrigger.onInteract.AddListener(SwitchPose);
        smokeTrigger.onInteract.AddListener(DropASmoke);
    }

    private void DropASmoke(PlayerControllerB playerInteracting)
    {
        if (playerInteracting == null || !playerInteracting.IsLocalPlayer()) return;
        SpawnTzpServerRpc();
    }

    [ServerRpc(RequireOwnership = false)]
    private void SpawnTzpServerRpc()
    {
        Item item = StartOfRound.Instance.allItemsList.itemsList.Where(x => x.itemName == "TZP-Inhalant").FirstOrDefault();
        if (item == null) return;

        GameObject tzp = Instantiate(item.spawnPrefab, smokeTransform.position, Quaternion.identity);
        tzp.GetComponent<NetworkObject>().Spawn();
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