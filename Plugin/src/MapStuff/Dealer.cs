using System;
using System.Collections;
using System.Diagnostics;
using GameNetcodeStuff;
using Unity.Netcode;
using Unity.Netcode.Components;
using UnityEngine;
using UnityEngine.Animations;

namespace CodeRebirth.MapStuff;
public class NetworkInteractable : NetworkBehaviour
{
    public Animator animator;
    public NetworkAnimator networkAnimator;
    public InteractTrigger trigger;
    public ParticleSystem[] particles;
    private PlayerControllerB playerWhoInteracted = null;
    private bool dealMade = false;

    public void Awake()
    {
        networkAnimator = GetComponent<NetworkAnimator>();
        animator = GetComponent<Animator>();
        trigger = GetComponent<InteractTrigger>();

        trigger.onInteract.AddListener(OnInteract);
    }

    [Conditional("DEBUG")]
    public void LogIfDebugBuild(object text)
    {
        Plugin.Logger.LogInfo(text);
    }

    private void OnInteract(PlayerControllerB playerInteracting)
    {
        if (playerInteracting != GameNetworkManager.Instance.localPlayerController) return;
        StartInteractionAnimationServerRpc(Array.IndexOf(StartOfRound.Instance.allPlayerScripts, playerInteracting));
    }

    [ServerRpc(RequireOwnership = false)]
    private void StartInteractionAnimationServerRpc(int playerThatInteracted)
    {
        LogIfDebugBuild("Interaction animation");
        SetInteractPlayerClientRpc(playerThatInteracted);
        animator.SetTrigger("devilInteract");
        // Run an animation event.
    }

    [ClientRpc]
    private void SetInteractPlayerClientRpc(int playerThatInteracted)
    {
        LogIfDebugBuild($"SetInteractPlayerClientRpc: {playerThatInteracted}");
        playerWhoInteracted = StartOfRound.Instance.allPlayerScripts[playerThatInteracted];
    }

    private void InteractAnimationResult() {
        if (playerWhoInteracted == GameNetworkManager.Instance.localPlayerController) {
            StartCoroutine(InteractionAnimation());
        }
        foreach (ParticleSystem particle in particles) {
            particle.gameObject.SetActive(true);
        }
        // start fire particles coming out from the devil, with an animation with some sounds and then a buff/debuff is dealt.
    }

    private IEnumerator InteractionAnimation() {
        yield return new WaitUntil(() => dealMade == true);
    }

    #region Good Deals
    public void IncreaseMovementSpeed() {
        LogIfDebugBuild("IncreaseMovementSpeed");
    }

    [ServerRpc(RequireOwnership = false)]
    public void SpawnShotgunWithShellsServerRpc() {
        LogIfDebugBuild("SpawnShotgunWithShellsServerRpc");
    }

    public void IncreaseStamina() {
        LogIfDebugBuild("IncreaseStamina");
    }

    public void IncreaseHealth() {
        LogIfDebugBuild("IncreaseHealth");
    }

    public void DecreaseMoonPrices() {
        LogIfDebugBuild("DecreaseMoonPrices");
    }

    [ServerRpc(RequireOwnership = false)]
    public void SpawnGoldbarServerRpc() {
        LogIfDebugBuild("SpawnGoldbarServerRpc");
    }

    public void IncreaseCarrySlotNumber() {
        LogIfDebugBuild("IncreaseCarrySlotNumber");
    }
    #endregion

    #region Bad Deals
    public void DecreaseMovementSpeed() {
        LogIfDebugBuild("DecreaseMovementSpeed");
    }

    [ServerRpc(RequireOwnership = false)]
    public void StealCreditOrScrapServerRpc() {
        LogIfDebugBuild("StealCreditOrScrap");
    }

    public void DecreaseStamina() {
        LogIfDebugBuild("DecreaseStamina");
    }

    public void DecreaseHealth() {
        LogIfDebugBuild("DecreaseHealth");
    }

    public void IncreaseMoonPrices() {
        LogIfDebugBuild("IncreaseMoonPrices");
    }

    public void DecreaseCarrySlotNumber() {
        LogIfDebugBuild("DecreaseCarrySlotNumber");
    }

    public void DealPlayerDamage() {
        LogIfDebugBuild("DealPlayerDamage");
    }
    #endregion
}