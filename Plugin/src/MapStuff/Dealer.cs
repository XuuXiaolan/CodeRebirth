using System;
using System.Collections;
using System.Collections.Generic;
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
    public InteractTrigger[] cardTriggers = null!;
    public Renderer[] cardRenderers = null!;
    public ParticleSystem mainParticles = null!;
    public ParticleSystem[] handParticles = null!;
    private PlayerControllerB? playerWhoInteracted = null;
    private bool dealMade = false;

    public enum CardNumber {
        One,
        Two,
        Three
    }

    public enum PositiveEffect {
        IncreaseMovementSpeed,
        SpawnShotgunWithShells,
        IncreaseStamina,
        IncreaseHealth,
        DecreaseMoonPrices,
        SpawnGoldBar,
        IncreaseCarrySlotNumber
    }

    public enum NegativeEffect {
        DecreaseMovementSpeed,
        StealCreditOrScrap,
        DecreaseStamina,
        DecreaseHealth,
        IncreaseMoonPrices,
        DecreaseCarrySlotNumber,
        DealPlayerDamage
    }

    private Dictionary<CardNumber, string> cardValues = new Dictionary<CardNumber, string>();

    private List<PositiveEffect> positiveEffects = new List<PositiveEffect>
    {
        PositiveEffect.IncreaseMovementSpeed,
        PositiveEffect.SpawnShotgunWithShells,
        PositiveEffect.IncreaseStamina,
        PositiveEffect.IncreaseHealth,
        PositiveEffect.DecreaseMoonPrices,
        PositiveEffect.SpawnGoldBar,
        PositiveEffect.IncreaseCarrySlotNumber
    };

    private List<NegativeEffect> negativeEffects = new List<NegativeEffect>
    {
        NegativeEffect.DecreaseMovementSpeed,
        NegativeEffect.StealCreditOrScrap,
        NegativeEffect.DecreaseStamina,
        NegativeEffect.DecreaseHealth,
        NegativeEffect.IncreaseMoonPrices,
        NegativeEffect.DecreaseCarrySlotNumber,
        NegativeEffect.DealPlayerDamage
    };

    public void Awake()
    {
        cardValues.Add(CardNumber.One, "PlaceholderOne");
        cardValues.Add(CardNumber.Two, "PlaceholderTwo");
        cardValues.Add(CardNumber.Three, "PlaceholderThree");
        networkAnimator = GetComponent<NetworkAnimator>();
        animator = GetComponent<Animator>();

        foreach (Renderer cardRenderer in cardRenderers) {
            cardRenderer.enabled = true;
        }

        foreach (InteractTrigger cardTrigger in cardTriggers) {
            cardTrigger.interactable = false;
        }
        
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
        // enable 3 other card triggers.
        int index = 0;
        foreach (InteractTrigger cardTrigger in cardTriggers) {
            cardTrigger.interactable = true;
            switch (index) {
                case 0:
                    cardValues[CardNumber.One] = GetStringEffects();
                    cardTrigger.onInteract.AddListener(Card1InteractionResult);
                    break;
                case 1:
                    cardTrigger.onInteract.AddListener(Card2InteractionResult);
                    break;
                case 2:
                    cardTrigger.onInteract.AddListener(Card3InteractionResult);
                    break;
            }
            index++;
        }
        foreach (Renderer cardRenderer in cardRenderers) {
            cardRenderer.enabled = true;
        }
    }

    public string GetStringEffects() {
        var random = new System.Random();
        PositiveEffect positiveEffect = positiveEffects[random.Next(positiveEffects.Count)];
        NegativeEffect negativeEffect = negativeEffects[random.Next(negativeEffects.Count)];
        
        return $"{positiveEffect} and {negativeEffect}";
    }

    private void Card1InteractionResult(PlayerControllerB playerThatInteracted) {
        StartInteractionAnimationServerRpc(Array.IndexOf(StartOfRound.Instance.allPlayerScripts, playerThatInteracted));
    }

    private void Card2InteractionResult(PlayerControllerB playerThatInteracted) {
        StartInteractionAnimationServerRpc(Array.IndexOf(StartOfRound.Instance.allPlayerScripts, playerThatInteracted));
    }

    private void Card3InteractionResult(PlayerControllerB playerThatInteracted) {
        StartInteractionAnimationServerRpc(Array.IndexOf(StartOfRound.Instance.allPlayerScripts, playerThatInteracted));
    }

    [ServerRpc(RequireOwnership = false)]
    private void StartInteractionAnimationServerRpc(int playerThatInteracted)
    {
        LogIfDebugBuild("Interaction animation");
        SetInteractPlayerClientRpc(playerThatInteracted);
        networkAnimator.SetTrigger("devilHandsInteract");
        // Run an animation event.
    }

    [ClientRpc]
    private void SetInteractPlayerClientRpc(int playerThatInteracted)
    {
        LogIfDebugBuild($"SetInteractPlayerClientRpc: {playerThatInteracted}");
        trigger.interactable = false;
        playerWhoInteracted = StartOfRound.Instance.allPlayerScripts[playerThatInteracted];
    }

    private void InteractAnimationResult() { // Animation event
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

    private void ExecutePositiveEffect(PositiveEffect effect) {
        switch (effect) {
            case PositiveEffect.IncreaseMovementSpeed:
                IncreaseMovementSpeed();
                break;
            case PositiveEffect.SpawnShotgunWithShells:
                SpawnShotgunWithShellsServerRpc();
                break;
            case PositiveEffect.IncreaseStamina:
                IncreaseStamina();
                break;
            case PositiveEffect.IncreaseHealth:
                IncreaseHealth();
                break;
            case PositiveEffect.DecreaseMoonPrices:
                DecreaseMoonPrices();
                break;
            case PositiveEffect.SpawnGoldBar:
                SpawnGoldbarServerRpc();
                break;
            case PositiveEffect.IncreaseCarrySlotNumber:
                IncreaseCarrySlotNumber();
                break;
        }
    }

    private void ExecuteNegativeEffect(NegativeEffect effect) {
        switch (effect) {
            case NegativeEffect.DecreaseMovementSpeed:
                DecreaseMovementSpeed();
                break;
            case NegativeEffect.StealCreditOrScrap:
                StealCreditOrScrapServerRpc();
                break;
            case NegativeEffect.DecreaseStamina:
                DecreaseStamina();
                break;
            case NegativeEffect.DecreaseHealth:
                DecreaseHealth();
                break;
            case NegativeEffect.IncreaseMoonPrices:
                IncreaseMoonPrices();
                break;
            case NegativeEffect.DecreaseCarrySlotNumber:
                DecreaseCarrySlotNumber();
                break;
            case NegativeEffect.DealPlayerDamage:
                DealPlayerDamage();
                break;
        }
    }
}
