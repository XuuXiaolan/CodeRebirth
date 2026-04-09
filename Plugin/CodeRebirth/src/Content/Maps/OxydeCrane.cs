using System.Collections;
using System.Collections.Generic;
using Dawn.Internal;
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

    public void Awake()
    {
        StartOfRound.Instance.StartNewRoundEvent.AddListener(OnStartNewRound);
        dropButton.gameObject.SetActive(false);
        StartMatchLeverRefs.Instance.triggerScript.interactable = false;
        previousDisabledTriggerMessage = StartMatchLeverRefs.Instance.triggerScript.disabledHoverTip;
        StartMatchLeverRefs.Instance.triggerScript.disabledHoverTip = "Ship needs to be un-attached from the magnet.";
        // stop ship lever from being unable to be pulled until ship's already been dropped.
        StartCoroutine(SpawnOutsideHazards());
    }

    private IEnumerator SpawnOutsideHazards()
    {
        yield return new WaitUntil(() => RoundManager.Instance.dungeonCompletedGenerating && RoundManager.Instance.mapPropsContainer != null);
        RoundManager.Instance.SpawnOutsideHazards();
    }

    private void OnStartNewRound()
    {
        craneAnimator.SetTrigger(StartAnimation);
        StartOfRound.Instance.StartNewRoundEvent.RemoveListener(OnStartNewRound);
    }

    public void Update()
    {
        RoundManager.Instance.currentDungeonType = -1;
        StartMatchLeverRefs.Instance.triggerScript.interactable = alreadyDropped;
    }

    public void DropInteract(PlayerControllerB player)
    {
        if (!player.IsLocalPlayer() || alreadyDropped)
        {
            return;
        }

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
        alreadyDropped = true;
        dropButton.interactable = false;
        StartMatchLeverRefs.Instance.triggerScript.disabledHoverTip = previousDisabledTriggerMessage;
        StartMatchLeverRefs.Instance.triggerScript.interactable = true;
        leverAnimator.SetTrigger(PullLeverAnimation);
        float timeElapsed = 0f;
        List<(GameObject gameObject, Vector3 startPos, Quaternion startRot)> objectsToDrop = [(shipNavColliders, shipNavColliders.transform.position, shipNavColliders.transform.rotation), (StartOfRound.Instance.shipAnimatorObject.gameObject, StartOfRound.Instance.shipAnimatorObject.transform.position, StartOfRound.Instance.shipAnimatorObject.transform.rotation)];
        yield return new WaitForSeconds(0.5f);
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

        float forceDuration = 5f;
        while (forceDuration > 0f)
        {
            yield return null;
            forceDuration -= Time.deltaTime;
            foreach (var tuple in objectsToDrop)
            {
                tuple.gameObject.transform.SetPositionAndRotation(endPosition, endRotation);
            }
        }

        craneAnimator.SetTrigger(MoveBackAnimation);
    }

    public void AllowDropInteract()
    {
        if (alreadyDropped)
        {
            return;
        }

        dropButton.gameObject.SetActive(true);
    }

    public override void OnNetworkDespawn()
    {
        base.OnNetworkDespawn();
        if (!alreadyDropped)
        {
            StartMatchLeverRefs.Instance.triggerScript.disabledHoverTip = previousDisabledTriggerMessage;
            StartMatchLeverRefs.Instance.triggerScript.interactable = true;
        }
    }
}