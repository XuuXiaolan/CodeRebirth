using System.Collections;
using System.Collections.Generic;
using CodeRebirth.src.Util;
using Dawn.Utils;
using GameNetcodeStuff;
using Unity.Netcode;
using UnityEngine;

namespace CodeRebirth.src.Content.Maps;
public class OxydeCrane : NetworkBehaviour
{
    public GameObject shipNavColliders = null!;
    public Vector3 endPosition = Vector3.zero;
    public Quaternion endRotation = Quaternion.identity;
    public Animator craneAnimator = null!;
    public Animator leverAnimator = null!;
    public InteractTrigger dropButton = null!;

    private string previousDisabledTriggerMessage = "";
    private bool alreadyDropped = false;
    private static readonly int StartAnimation = Animator.StringToHash("startAnimation"); // Trigger
    private static readonly int MoveBackAnimation = Animator.StringToHash("MoveBack");
    private static readonly int PullLeverAnimation = Animator.StringToHash("pullLever");
    [HideInInspector] public static OxydeCrane? Instance = null;

    public void Awake()
    {
        Instance = this;
        CodeRebirthUtils.Instance.startMatchLever.triggerScript.interactable = false;
        previousDisabledTriggerMessage = CodeRebirthUtils.Instance.startMatchLever.triggerScript.disabledHoverTip;
        CodeRebirthUtils.Instance.startMatchLever.triggerScript.disabledHoverTip = "Ship needs to be un-attached from the magnet.";
        // stop ship lever from being unable to be pulled until ship's already been dropped.
        StartCoroutine(SpawnOutsideHazards());
        StartCoroutine(WaitUntilShipLoads());
    }

    private IEnumerator SpawnOutsideHazards()
    {
        yield return new WaitUntil(() => RoundManager.Instance.dungeonCompletedGenerating && RoundManager.Instance.mapPropsContainer != null);
        RoundManager.Instance.SpawnOutsideHazards();
    }

    private IEnumerator WaitUntilShipLoads()
    {
        yield return new WaitUntil(() => !StartOfRound.Instance.inShipPhase && this.NetworkObject.IsSpawned);
        craneAnimator.SetTrigger(StartAnimation);
    }

    public void Update()
    {
        RoundManager.Instance.currentDungeonType = -1;
        CodeRebirthUtils.Instance.startMatchLever.triggerScript.interactable = !dropButton.interactable;
    }

    public void DropInteract(PlayerControllerB player)
    {
        if (!player.IsLocalPlayer() || alreadyDropped) return;
        TryDropShipFromCraneServerRpc();
    }

    [ServerRpc(RequireOwnership = false)]
    private void TryDropShipFromCraneServerRpc()
    {
        TryDropShipFromCraneClientRpc();
    }

    [ClientRpc]
    private void TryDropShipFromCraneClientRpc()
    {
        StartCoroutine(TryDropShipFromCrane());
    }

    public IEnumerator TryDropShipFromCrane()
    {
        dropButton.interactable = false;
        CodeRebirthUtils.Instance.startMatchLever.triggerScript.disabledHoverTip = previousDisabledTriggerMessage;
        CodeRebirthUtils.Instance.startMatchLever.triggerScript.interactable = true;
        leverAnimator.SetTrigger(PullLeverAnimation);
        float timeElapsed = 0f;
        List<(GameObject gameObject, Vector3 startPos, Quaternion startRot)> objectsToDrop = [(shipNavColliders, shipNavColliders.transform.position, shipNavColliders.transform.rotation), (StartOfRound.Instance.shipAnimatorObject.gameObject, StartOfRound.Instance.shipAnimatorObject.transform.position, StartOfRound.Instance.shipAnimatorObject.transform.rotation)];
        while (timeElapsed <= 2f)
        {
            yield return null;
            timeElapsed += Time.deltaTime;
            foreach (var tuple in objectsToDrop)
            {
                Vector3 vector = Vector3.Lerp(tuple.startPos, endPosition, timeElapsed);
                Quaternion quaternion = Quaternion.Lerp(tuple.startRot, endRotation, timeElapsed);
                tuple.gameObject.transform.SetPositionAndRotation(vector, quaternion);
            }
        }

        craneAnimator.SetTrigger(MoveBackAnimation);
    }

    public override void OnNetworkDespawn()
    {
        base.OnNetworkDespawn();
        if (!alreadyDropped)
        {
            CodeRebirthUtils.Instance.startMatchLever.triggerScript.disabledHoverTip = previousDisabledTriggerMessage;
            CodeRebirthUtils.Instance.startMatchLever.triggerScript.interactable = true;
        }
        Instance = null;
    }
}