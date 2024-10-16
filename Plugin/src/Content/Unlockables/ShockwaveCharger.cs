using System;
using System.Collections;
using System.Linq;
using GameNetcodeStuff;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace CodeRebirth.src.Content.Unlockables;
public class ShockwaveCharger : NetworkBehaviour
{
    public GameObject ShockwaveGal = null!;
    public InteractTrigger ActivateOrDeactivateTrigger = null!;
    public Transform ChargeTransform = null!;

    private ShockwaveGalAI shockwaveGalAI = null!;

    public void Start()
    {
        if (IsServer)
        {
            GameObject shockwaveGal = GameObject.Instantiate(ShockwaveGal);
			SceneManager.MoveGameObjectToScene(shockwaveGal, StartOfRound.Instance.gameObject.scene);
            shockwaveGal.GetComponent<NetworkObject>().Spawn();
            SetGalForEveryoneClientRpc(new NetworkObjectReference(shockwaveGal.GetComponent<NetworkObject>()));
        }
        if (Plugin.ModConfig.ConfigShockwaveBotAutomatic.Value) StartCoroutine(ActivateShockwaveGalAfterLand());
        ActivateOrDeactivateTrigger.onInteract.AddListener(OnActivateShockwaveGal);
    }

    private IEnumerator ActivateShockwaveGalAfterLand()
    {
        while (true)
        {
            yield return new WaitUntil(() => TimeOfDay.Instance.normalizedTimeOfDay <= 0.2f && StartOfRound.Instance.shipHasLanded && !shockwaveGalAI.Animator.GetBool("activated") && !StartOfRound.Instance.shipIsLeaving && !StartOfRound.Instance.inShipPhase && RoundManager.Instance.currentLevel.levelID != 3);
            if (!shockwaveGalAI.Animator.GetBool("activated"))
            {
                PlayerControllerB closestPlayer = StartOfRound.Instance.allPlayerScripts.Where(p => p.isPlayerControlled).OrderBy(p => Vector3.Distance(transform.position, p.transform.position)).First();
                shockwaveGalAI.ActivateShockwaveGal(closestPlayer);
            }
        }
    }

    [ClientRpc]
    private void SetGalForEveryoneClientRpc(NetworkObjectReference networkObjectReference)
    {
        shockwaveGalAI = ((GameObject)networkObjectReference).GetComponent<ShockwaveGalAI>();
        shockwaveGalAI.ShockwaveCharger = this;
    }

    private void OnActivateShockwaveGal(PlayerControllerB playerInteracting)
    {
        if (playerInteracting == null || playerInteracting != GameNetworkManager.Instance.localPlayerController) return;
        if (StartOfRound.Instance.inShipPhase || !StartOfRound.Instance.shipHasLanded || StartOfRound.Instance.shipIsLeaving || (RoundManager.Instance.currentLevel.levelID == 3 && TimeOfDay.Instance.quotaFulfilled >= TimeOfDay.Instance.profitQuota)) return;
        if (!shockwaveGalAI.Animator.GetBool("activated")) ActivateGirlServerRpc(Array.IndexOf(StartOfRound.Instance.allPlayerScripts, playerInteracting));
        else ActivateGirlServerRpc(-1);
    }

    [ServerRpc(RequireOwnership = false)]
    public void ActivateGirlServerRpc(int index)
    {
        shockwaveGalAI.Animator.SetBool("activated", index != -1);
        ActivateGirlClientRpc(index);
    }

    [ClientRpc]
    private void ActivateGirlClientRpc(int index)
    {
        if (index != -1) shockwaveGalAI.ActivateShockwaveGal(StartOfRound.Instance.allPlayerScripts[index]);
        else shockwaveGalAI.DeactivateShockwaveGal();
    }
}