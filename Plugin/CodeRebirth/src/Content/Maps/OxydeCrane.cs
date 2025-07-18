using System.Collections;
using System.Linq;
using CodeRebirth.src.Util;
using CodeRebirth.src.Util.Extensions;
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
        StartCoroutine(TryDropShipFromCrane());
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
        if (alreadyDropped)
        {
            yield break;
        }
        dropButton.interactable = false;
        CodeRebirthUtils.Instance.startMatchLever.triggerScript.disabledHoverTip = previousDisabledTriggerMessage;
        CodeRebirthUtils.Instance.startMatchLever.triggerScript.interactable = true;
        leverAnimator.SetTrigger(PullLeverAnimation);
        alreadyDropped = true;
        float timeElapsed = 0f;
        GameObject[] objectsToDrop = [shipNavColliders, StartOfRound.Instance.shipAnimatorObject.gameObject];
        Plugin.ExtendedLogging($"Dropping the following objects from the crane: {string.Join(", ", objectsToDrop.Select(x => x.name))}");
        while (timeElapsed <= 0.5f)
        {
            yield return null;
            timeElapsed += Time.deltaTime;
            foreach (GameObject obj in objectsToDrop)
            {
                Vector3 vector = Vector3.Lerp(obj.transform.position, endPosition, timeElapsed * 2f);
                Quaternion quaternion = Quaternion.Lerp(obj.transform.rotation, endRotation, timeElapsed * 2f);
                obj.transform.SetPositionAndRotation(vector, quaternion);
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