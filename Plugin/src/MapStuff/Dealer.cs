using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using CodeRebirth.Util.Spawning;
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
    private bool dealMade = false;
    private bool particlesEnabled = false;
    private PlayerControllerB? playerWhoCanDoShit = null;

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

    private Dictionary<CardNumber, (PositiveEffect positive, NegativeEffect negative)> cardEffects = new Dictionary<CardNumber, (PositiveEffect, NegativeEffect)>();

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

    private System.Random random = new System.Random();
    public void Awake()
    {
        random = new System.Random(StartOfRound.Instance.randomMapSeed);
        networkAnimator = GetComponent<NetworkAnimator>();
        animator = GetComponent<Animator>();

        foreach (Renderer cardRenderer in cardRenderers) {
            cardRenderer.enabled = false;
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

        HandleInteractionForPlayerServerRpc(Array.IndexOf(StartOfRound.Instance.allPlayerScripts, playerInteracting));
    }

    [ServerRpc(RequireOwnership = false)]
    public void HandleInteractionForPlayerServerRpc(int playerThatInteracted) {
        HandleInteractionForPlayerClientRpc(playerThatInteracted);
    }

    [ClientRpc]
    public void HandleInteractionForPlayerClientRpc(int playerThatInteracted) {
        // enable 3 other card triggers.
        playerWhoCanDoShit = StartOfRound.Instance.allPlayerScripts[playerThatInteracted];
        int index = 0;
        foreach (InteractTrigger cardTrigger in cardTriggers) {
            cardTrigger.interactable = true;
            switch (index) {
                case 0:
                    cardEffects[CardNumber.One] = GetCardEffects();
                    cardTrigger.onInteract.AddListener(Card1InteractionResult);
                    cardTrigger.hoverTip = $"{cardEffects[CardNumber.One].positive} and {cardEffects[CardNumber.One].negative}";
                    break;
                case 1:
                    cardEffects[CardNumber.Two] = GetCardEffects();
                    cardTrigger.onInteract.AddListener(Card2InteractionResult);
                    cardTrigger.hoverTip = $"{cardEffects[CardNumber.Two].positive} and {cardEffects[CardNumber.Two].negative}";
                    break;
                case 2:
                    cardEffects[CardNumber.Three] = GetCardEffects();
                    cardTrigger.onInteract.AddListener(Card3InteractionResult);
                    cardTrigger.hoverTip = $"{cardEffects[CardNumber.Three].positive} and {cardEffects[CardNumber.Three].negative}";
                    break;
            }
            index++;
            cardTrigger.interactable = true;
        }
        foreach (Renderer cardRenderer in cardRenderers) {
            cardRenderer.enabled = true;
        }

        trigger.gameObject.SetActive(false);
    }


    public (PositiveEffect positive, NegativeEffect negative) GetCardEffects() {
        PositiveEffect positiveEffect = positiveEffects[random.Next(positiveEffects.Count)];
        NegativeEffect negativeEffect = negativeEffects[random.Next(negativeEffects.Count)];
        
        return (positiveEffect, negativeEffect);
    }

    [ServerRpc(RequireOwnership = false)]
    public void ThreeCardInteractionServerRpc(int cardPickedIndex) {
        ThreeCardInteractionClientRpc(cardPickedIndex);
    }

    [ClientRpc]
    public void ThreeCardInteractionClientRpc(int cardPickedIndex) {
        foreach (InteractTrigger cardTrigger in cardTriggers) {
            cardTrigger.interactable = false;
        }
        StartCoroutine(ThreeCardInteractionAnimation(cardPickedIndex));
    }

    private void Card1InteractionResult(PlayerControllerB playerThatInteracted) {
        if (playerThatInteracted != playerWhoCanDoShit) return;
        ThreeCardInteractionServerRpc(0);
        LogIfDebugBuild("Card1InteractionResult");
    }

    private void Card2InteractionResult(PlayerControllerB playerThatInteracted) {
        if (playerThatInteracted != playerWhoCanDoShit) return;
        ThreeCardInteractionServerRpc(1);
        LogIfDebugBuild("Card2InteractionResult");
    }

    private void Card3InteractionResult(PlayerControllerB playerThatInteracted) {
        if (playerThatInteracted != playerWhoCanDoShit) return;
        ThreeCardInteractionServerRpc(2);
        LogIfDebugBuild("Card3InteractionResult");
    }

    public void Update() {
        bool withinDistance = Vector3.Distance(transform.position, GameNetworkManager.Instance.localPlayerController.transform.position) <= 10;
        
        if (withinDistance && !particlesEnabled && !dealMade) {
            foreach (ParticleSystem particle in handParticles) {
                particle.Play();
            }
            LogIfDebugBuild("Enabling hand particles");
            particlesEnabled = true; // Update the state to enabled
        } else if ((!withinDistance || dealMade) && particlesEnabled) {
            foreach (ParticleSystem particle in handParticles) {
                particle.Stop();
            }
            LogIfDebugBuild("Disabling hand particles");
            particlesEnabled = false; // Update the state to disabled
        }
    }


    private IEnumerator ThreeCardInteractionAnimation(int cardPickedIndex) {
        
        mainParticles.Play();

        foreach (Renderer cardRenderer in cardRenderers) {
            cardRenderer.material.color = Color.black;
            cardRenderer.material.mainTexture = null;
        }
        yield return new WaitForSeconds(7f);
        dealMade = true;
        switch (cardPickedIndex) {
            case 0:
                ExecutePositiveEffect(cardEffects[CardNumber.One].positive);
                ExecuteNegativeEffect(cardEffects[CardNumber.One].negative);
                break;
            case 1:
                ExecutePositiveEffect(cardEffects[CardNumber.Two].positive);
                ExecuteNegativeEffect(cardEffects[CardNumber.Two].negative);
                break;
            case 2:
                ExecutePositiveEffect(cardEffects[CardNumber.Three].positive);
                ExecuteNegativeEffect(cardEffects[CardNumber.Three].negative);
                break;
        }
        LogIfDebugBuild("main particles animation done");
    }

    #region Good Deals
    public void IncreaseMovementSpeed() {
        LogIfDebugBuild("IncreaseMovementSpeed");
    }

    [ServerRpc(RequireOwnership = false)]
    public void SpawnShotgunWithShellsServerRpc() {
        Item Shotgun = StartOfRound.Instance.allItemsList.itemsList.Where(x => x.itemName == "Shotgun").First();
        GameObject spawned = Instantiate(Shotgun.spawnPrefab, transform.position + transform.up*1f + transform.forward* 1.5f, Quaternion.Euler(Shotgun.restingRotation), RoundManager.Instance.spawnedScrapContainer);

        GrabbableObject grabbableObject = spawned.GetComponent<GrabbableObject>();
        
        grabbableObject.SetScrapValue((int)(random.Next(Shotgun.minValue, Shotgun.maxValue) * RoundManager.Instance.scrapValueMultiplier));
        grabbableObject.NetworkObject.Spawn();
        CodeRebirthUtils.Instance.UpdateScanNodeClientRpc(new NetworkObjectReference(spawned), grabbableObject.scrapValue);
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
