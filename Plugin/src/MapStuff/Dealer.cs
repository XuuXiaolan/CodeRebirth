using System;
using System.Collections;
using System.Diagnostics;
using GameNetcodeStuff;
using Unity.Netcode;
using Unity.Netcode.Components;
using UnityEngine;

namespace CodeRebirth.MapStuff;
public class Dealer : NetworkBehaviour
{
    public Animator animator = null!;
    public NetworkAnimator networkAnimator = null!;
    public InteractTrigger trigger = null!;
    public ParticleSystem mainParticles = null!;
    public ParticleSystem[] handParticles = null!;
    private PlayerControllerB? playerWhoInteracted = null;
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
        StartCoroutine(InteractionAnimation());
        // start fire particles coming out from the devil, with an animation with some sounds and then a buff/debuff is dealt.
    }

    private IEnumerator InteractionAnimation() {
        foreach (ParticleSystem particle in handParticles) {
            particle.gameObject.SetActive(true);
            yield return new WaitForSeconds(0.5f);
        }

        mainParticles.gameObject.SetActive(true);
        yield return new WaitUntil(() => mainParticles.isPlaying == false);
        LogIfDebugBuild("Interaction animation done");
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