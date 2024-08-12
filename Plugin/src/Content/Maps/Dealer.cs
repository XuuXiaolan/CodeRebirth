using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using CodeRebirth.src.Util.Extensions;
using CodeRebirth.src.Util;
using GameNetcodeStuff;
using LethalLevelLoader;
using Unity.Netcode;
using Unity.Netcode.Components;
using UnityEngine;
using UnityEngine.UI;

namespace CodeRebirth.src.Content.Maps;
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
    public Transform ItemSpawnSpot = null!;

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
        IncreaseCarrySlotNumber,
        SpawnJetpack,
    }

    public enum NegativeEffect {
        DecreaseMovementSpeed,
        StealCreditOrScrap,
        DecreaseStamina,
        IncreaseMoonPrices,
        DecreaseCarrySlotNumber,
        DealPlayerDamage
    }

    private Dictionary<CardNumber, (PositiveEffect positive, NegativeEffect negative)> cardEffects = new Dictionary<CardNumber, (PositiveEffect, NegativeEffect)>();
    private bool cardPlayingAnimation = false;

    private List<PositiveEffect> positiveEffects = new List<PositiveEffect>
    {
        PositiveEffect.IncreaseMovementSpeed,
        PositiveEffect.SpawnShotgunWithShells,
        PositiveEffect.IncreaseStamina,
        PositiveEffect.IncreaseHealth,
        PositiveEffect.DecreaseMoonPrices,
        PositiveEffect.SpawnGoldBar,
        PositiveEffect.IncreaseCarrySlotNumber,
        PositiveEffect.SpawnJetpack,
    };

    private List<NegativeEffect> negativeEffects = new List<NegativeEffect>
    {
        NegativeEffect.DecreaseMovementSpeed,
        NegativeEffect.StealCreditOrScrap,
        NegativeEffect.DecreaseStamina,
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
        PositiveEffect positiveEffect = positiveEffects[random.NextInt(0, positiveEffects.Count-1)];
        NegativeEffect negativeEffect = negativeEffects[random.NextInt(0, negativeEffects.Count-1)];
        
        return (positiveEffect, negativeEffect);
    }

    [ServerRpc(RequireOwnership = false)]
    public void ThreeCardInteractionServerRpc(int cardPickedIndex, int playerModifiedIndex) {
        ThreeCardInteractionClientRpc(cardPickedIndex, playerModifiedIndex);
    }

    [ClientRpc]
    public void ThreeCardInteractionClientRpc(int cardPickedIndex, int playerModifiedIndex) {
        foreach (InteractTrigger cardTrigger in cardTriggers) {
            cardTrigger.interactable = false;
        }
        if (cardPickedIndex == -1 || playerModifiedIndex == -1) {
            StartCoroutine(ThreeCardInteractionAnimation(-1, null));
        } else {
            StartCoroutine(ThreeCardInteractionAnimation(cardPickedIndex, StartOfRound.Instance.allPlayerScripts[playerModifiedIndex]));
        }
    }

    private void Card1InteractionResult(PlayerControllerB? playerThatInteracted) {
        if (playerThatInteracted != playerWhoCanDoShit) return;
        ThreeCardInteractionServerRpc(0, Array.IndexOf(StartOfRound.Instance.allPlayerScripts, playerThatInteracted));
        Plugin.ExtendedLogging("Card1InteractionResult");
    }

    private void Card2InteractionResult(PlayerControllerB? playerThatInteracted) {
        if (playerThatInteracted != playerWhoCanDoShit) return;
        ThreeCardInteractionServerRpc(1, Array.IndexOf(StartOfRound.Instance.allPlayerScripts, playerThatInteracted));
        Plugin.ExtendedLogging("Card2InteractionResult");
    }

    private void Card3InteractionResult(PlayerControllerB? playerThatInteracted) {
        if (playerThatInteracted != playerWhoCanDoShit) return;
        ThreeCardInteractionServerRpc(2, Array.IndexOf(StartOfRound.Instance.allPlayerScripts, playerThatInteracted));
        Plugin.ExtendedLogging("Card3InteractionResult");
    }

    public void Update() {
        bool withinDistance = Vector3.Distance(transform.position, GameNetworkManager.Instance.localPlayerController.transform.position) <= 10;
        
        if (withinDistance && !particlesEnabled && !dealMade) {
            foreach (ParticleSystem particle in handParticles) {
                particle.Play();
            }
            Plugin.ExtendedLogging("Enabling hand particles");
            particlesEnabled = true; // Update the state to enabled
        } else if ((!withinDistance || dealMade) && particlesEnabled) {
            foreach (ParticleSystem particle in handParticles) {
                particle.Stop();
            }
            Plugin.ExtendedLogging("Disabling hand particles");
            particlesEnabled = false; // Update the state to disabled
        }

        if (cardPlayingAnimation && (Vector3.Distance(cardTriggers[0].gameObject.transform.position, cardTriggers[1].gameObject.transform.position) > 0.01f || Vector3.Distance(cardTriggers[2].gameObject.transform.position, cardTriggers[1].gameObject.transform.position) > 0.01f)) {
            cardTriggers[0].gameObject.transform.position = Vector3.Lerp(cardTriggers[0].gameObject.transform.position, cardTriggers[1].gameObject.transform.position, Time.deltaTime * 0.5f);
            cardTriggers[2].gameObject.transform.position = Vector3.Lerp(cardTriggers[2].gameObject.transform.position, cardTriggers[1].gameObject.transform.position, Time.deltaTime * 0.5f);
        }

        if (playerWhoCanDoShit != null && !dealMade && Vector3.Distance(playerWhoCanDoShit.transform.position, transform.position) > 10f && GameNetworkManager.Instance.localPlayerController == playerWhoCanDoShit) {
            playerWhoCanDoShit.DamagePlayer(99, true, false, CauseOfDeath.Burning, 0, false, default);
            playerWhoCanDoShit = null;
            ThreeCardInteractionServerRpc(-1, -1);
        }
    }

    private IEnumerator ThreeCardInteractionAnimation(int cardPickedIndex, PlayerControllerB? playerModified) {
        
        mainParticles.Play();

        foreach (Renderer cardRenderer in cardRenderers) {
            cardRenderer.material.color = Color.black;
            cardRenderer.material.mainTexture = null;
        }
        cardPlayingAnimation = true;
        yield return new WaitForSeconds(7f);
        dealMade = true;
        if (playerModified == null || cardPickedIndex == -1) {
            yield break;
        }
        switch (cardPickedIndex) {
            case 0:
                ExecutePositiveEffect(cardEffects[CardNumber.One].positive, playerModified);
                ExecuteNegativeEffect(cardEffects[CardNumber.One].negative, playerModified);
                break;
            case 1:
                ExecutePositiveEffect(cardEffects[CardNumber.Two].positive, playerModified);
                ExecuteNegativeEffect(cardEffects[CardNumber.Two].negative, playerModified);
                break;
            case 2:
                ExecutePositiveEffect(cardEffects[CardNumber.Three].positive, playerModified);
                ExecuteNegativeEffect(cardEffects[CardNumber.Three].negative, playerModified);
                break;
        }
        Plugin.ExtendedLogging("main particles animation done");
    }

    #region Good Deals
    public void IncreaseMovementSpeed(PlayerControllerB? playerModified) {
        if (playerModified == null) {
            Plugin.Logger.LogError("playerModified is null");
            return;
        }
        playerModified.movementSpeed *= 1.25f;
        Plugin.ExtendedLogging("IncreaseMovementSpeed");
    }

    [ServerRpc(RequireOwnership = false)]
    public void SpawnShotgunWithShellsServerRpc() {
        Item Shotgun = StartOfRound.Instance.allItemsList.itemsList.Where(x => x.itemName == "Shotgun").First();
        GameObject spawned = Instantiate(Shotgun.spawnPrefab, ItemSpawnSpot.position, Quaternion.Euler(Shotgun.restingRotation), RoundManager.Instance.spawnedScrapContainer);

        GrabbableObject grabbableObject = spawned.GetComponent<GrabbableObject>();
        
        grabbableObject.SetScrapValue((int)(random.NextInt(Shotgun.minValue, Shotgun.maxValue) * RoundManager.Instance.scrapValueMultiplier));
        grabbableObject.NetworkObject.Spawn();
        CodeRebirthUtils.Instance.UpdateScanNodeClientRpc(new NetworkObjectReference(spawned), grabbableObject.scrapValue);
        grabbableObject.gameObject.transform.position = ItemSpawnSpot.position;

        Item ShotgunAmmo = StartOfRound.Instance.allItemsList.itemsList.Where(x => x.itemName == "Ammo").First();
        for (int i = 0; i < 3; i++) {
            GameObject spawnedAmmo = Instantiate(ShotgunAmmo.spawnPrefab, ItemSpawnSpot.position, Quaternion.Euler(ShotgunAmmo.restingRotation), RoundManager.Instance.spawnedScrapContainer);
            GrabbableObject grabbableObjectAmmo = spawnedAmmo.GetComponent<GrabbableObject>();
            grabbableObjectAmmo.NetworkObject.Spawn();

            grabbableObjectAmmo.gameObject.transform.position = ItemSpawnSpot.position;
        }
        Plugin.ExtendedLogging("SpawnShotgunWithShellsServerRpc");
    }

    public void IncreaseStamina(PlayerControllerB? playerModified) {
        if (playerModified == null) {
            Plugin.Logger.LogError("playerModified is null");
            return;
        }
        playerModified.sprintTime += 5f;
        Plugin.ExtendedLogging("IncreaseStamina");
    }

    public void IncreaseHealth(PlayerControllerB? playerModified) {
        if (playerModified == null) {
            Plugin.Logger.LogError("playerModified is null");
            return;
        }

        playerModified.DamagePlayer(playerModified.health - 125);
        Plugin.ExtendedLogging("IncreaseHealth");
    }

    public void DecreaseMoonPrices() {
        foreach (ExtendedLevel extendedLevel in LethalLevelLoader.PatchedContent.ExtendedLevels) {
            extendedLevel.RoutePrice = (int)(extendedLevel.RoutePrice /1.2f);
        }

        Plugin.ExtendedLogging("DecreaseMoonPrices");
    }

    [ServerRpc(RequireOwnership = false)]
    public void SpawnGoldbarServerRpc() {
        Item GoldBar = StartOfRound.Instance.allItemsList.itemsList.Where(x => x.itemName == "Gold bar").First();
        GameObject spawned = Instantiate(GoldBar.spawnPrefab, ItemSpawnSpot.position, Quaternion.Euler(GoldBar.restingRotation), RoundManager.Instance.spawnedScrapContainer);

        GrabbableObject grabbableObject = spawned.GetComponent<GrabbableObject>();
        
        grabbableObject.SetScrapValue((int)(random.NextInt(GoldBar.minValue, GoldBar.maxValue) * RoundManager.Instance.scrapValueMultiplier));
        grabbableObject.NetworkObject.Spawn();
        grabbableObject.gameObject.transform.position = ItemSpawnSpot.position;
        CodeRebirthUtils.Instance.UpdateScanNodeClientRpc(new NetworkObjectReference(spawned), grabbableObject.scrapValue);
        Plugin.ExtendedLogging("SpawnGoldbarServerRpc");
    }

    [ServerRpc(RequireOwnership = false)]
    public void SpawnJetpackServerRpc() {
        Item Jetpack = StartOfRound.Instance.allItemsList.itemsList.Where(x => x.itemName == "Jetpack").First();
        GameObject spawned = Instantiate(Jetpack.spawnPrefab, ItemSpawnSpot.position, Quaternion.Euler(Jetpack.restingRotation), RoundManager.Instance.spawnedScrapContainer);

        GrabbableObject grabbableObject = spawned.GetComponent<GrabbableObject>();
        
        grabbableObject.NetworkObject.Spawn();
        grabbableObject.gameObject.transform.position = ItemSpawnSpot.position;
        Plugin.ExtendedLogging("SpawnJetpackServerRpc");
    }

    public void IncreaseCarrySlotNumber(PlayerControllerB? playerModified) {
        if (playerModified == null) {
            Plugin.Logger.LogError("playerModified is null");
            return;
        }
        playerModified.DropAllHeldItems();
        var itemSlots = playerModified.ItemSlots.ToList();

        playerModified.ItemSlots = new GrabbableObject[itemSlots.Count + 1];

        if (playerModified == GameNetworkManager.Instance.localPlayerController)
        {
            UpdateHUD(true);
        }
        Plugin.ExtendedLogging("IncreaseCarrySlotNumber");
    }
    #endregion

    #region Bad Deals
    public void DecreaseMovementSpeed(PlayerControllerB? playerModified) {
        if (playerModified == null) {
            Plugin.Logger.LogError("playerModified is null");
            return;
        }
        playerModified.movementSpeed /= 1.25f;
        Plugin.ExtendedLogging("DecreaseMovementSpeed");
    }

    [ServerRpc(RequireOwnership = false)]
    public void StealCreditOrScrapServerRpc() {
        DetermineScrapToDestroyOrCreditsToSteal();
        Plugin.ExtendedLogging("StealCreditOrScrap");
    }

    public void DecreaseStamina(PlayerControllerB? playerModified) {
        if (playerModified == null) {
            Plugin.Logger.LogError("playerModified is null");
            return;
        }
        if (playerModified.sprintTime < 1f) return;
        playerModified.sprintTime = Mathf.Clamp(playerModified.sprintTime - 2.5f, 1f, 1000f);;
        Plugin.ExtendedLogging("DecreaseStamina");
    }

    public void IncreaseMoonPrices() {
        foreach (ExtendedLevel extendedLevel in LethalLevelLoader.PatchedContent.ExtendedLevels) {
            extendedLevel.RoutePrice = (int)(extendedLevel.RoutePrice*1.2f);
        }
        Plugin.ExtendedLogging("IncreaseMoonPrices");
    }

    public void DecreaseCarrySlotNumber(PlayerControllerB? playerModified) {
        if (playerModified == null) {
            Plugin.Logger.LogError("playerModified is null");
            return;
        }
        playerModified.DropAllHeldItems();
        var itemSlots = playerModified.ItemSlots.ToList();

        playerModified.ItemSlots = new GrabbableObject[itemSlots.Count - 1];

        if (playerModified == GameNetworkManager.Instance.localPlayerController)
        {
            UpdateHUD(false);
        }
        Plugin.ExtendedLogging("DecreaseCarrySlotNumber");
    }

    public void DealPlayerDamage(PlayerControllerB? playerModified) {
        if (playerModified == null) {
            Plugin.Logger.LogError("playerModified is null");
            return;
        }
        playerModified.DamagePlayer(50, true, false, CauseOfDeath.Burning, 0, false, default);
        Plugin.ExtendedLogging("DealPlayerDamage");
    }
    #endregion

    private void DetermineScrapToDestroyOrCreditsToSteal() {
        int credits = FindObjectOfType<Terminal>().groupCredits;
        int newCredits = 0;
        List<GrabbableObject> scrapInShip = GameObject.Find("/Environment/HangarShip").GetComponentsInChildren<GrabbableObject>().Where(x => x.itemProperties.isScrap).ToList();
        if (credits > 700) {
            newCredits = (int)(credits * 0.8f);
        }
        if (newCredits > 0) {
            UpdateCreditsClientRpc(newCredits);
        } else if (scrapInShip.Count >= 3) {
            for (int i = 0; i < 3; i++) {
                int randomIndex = UnityEngine.Random.Range(0, scrapInShip.Count);
                GrabbableObject scrapToDespawn = scrapInShip[randomIndex];
                scrapInShip.RemoveAt(randomIndex);
                NetworkObject networkObject = scrapToDespawn.GetComponent<NetworkObject>();
                if (networkObject != null && networkObject.IsSpawned) {
                    networkObject.Despawn();
                }
            }
            UpdateCurrentShipItemCountClientRpc();
        } else {
            if (UnityEngine.Random.Range(0f, 100f) > 95) {
                UpdateCreditsClientRpc(666);   
            } else {
                UpdateCreditsClientRpc(0);
            }
        }
    }

    [ClientRpc]
    private void UpdateCurrentShipItemCountClientRpc() {
        StartOfRound.Instance.currentShipItemCount -= 3;
    }

    [ClientRpc]
    private void UpdateCreditsClientRpc(int credits) {
        FindObjectOfType<Terminal>().groupCredits = credits;
    }
    private void ExecutePositiveEffect(PositiveEffect effect, PlayerControllerB? playerModified) {
        switch (effect) {
            case PositiveEffect.IncreaseMovementSpeed:
                IncreaseMovementSpeed(playerModified);
                break;
            case PositiveEffect.SpawnShotgunWithShells:
                if (GameNetworkManager.Instance.localPlayerController == playerModified) SpawnShotgunWithShellsServerRpc();
                break;
            case PositiveEffect.IncreaseStamina:
                IncreaseStamina(playerModified);
                break;
            case PositiveEffect.SpawnJetpack:
                if (GameNetworkManager.Instance.localPlayerController == playerModified) SpawnJetpackServerRpc();
                break;
            case PositiveEffect.IncreaseHealth:
                IncreaseHealth(playerModified);
                break;
            case PositiveEffect.DecreaseMoonPrices:
                DecreaseMoonPrices();
                break;
            case PositiveEffect.SpawnGoldBar:
                if (GameNetworkManager.Instance.localPlayerController == playerModified) SpawnGoldbarServerRpc();
                break;
            case PositiveEffect.IncreaseCarrySlotNumber:
                IncreaseCarrySlotNumber(playerModified);
                break;
        }
    }

    private void ExecuteNegativeEffect(NegativeEffect effect, PlayerControllerB? playerModified) {
        switch (effect) {
            case NegativeEffect.DecreaseMovementSpeed:
                DecreaseMovementSpeed(playerModified);
                break;
            case NegativeEffect.StealCreditOrScrap:
                if (GameNetworkManager.Instance.localPlayerController == playerModified) StealCreditOrScrapServerRpc();
                break;
            case NegativeEffect.DecreaseStamina:
                DecreaseStamina(playerModified);
                break;
            case NegativeEffect.IncreaseMoonPrices:
                IncreaseMoonPrices();
                break;
            case NegativeEffect.DecreaseCarrySlotNumber:
                DecreaseCarrySlotNumber(playerModified);
                break;
            case NegativeEffect.DealPlayerDamage:
                DealPlayerDamage(playerModified);
                break;
        }
    }

    public List<int> slotIndexes = new List<int>();

    public void UpdateHUD(bool add)
    {
        slotIndexes.Clear();
        var hud = HUDManager.Instance;
        if (add)
        {
            var referenceFrame = hud.itemSlotIconFrames[0];
            var referenceIcon = hud.itemSlotIcons[0];

            var lastInventorySize = hud.itemSlotIconFrames.Length;

            var slotSizeX = referenceFrame.rectTransform.sizeDelta.x;
            var slotSizeY = referenceFrame.rectTransform.sizeDelta.y;
            var yPosition = referenceFrame.rectTransform.anchoredPosition.y + 1.125f * slotSizeY;
            var frameAngles = referenceFrame.rectTransform.localEulerAngles;
            var iconAngles = referenceIcon.rectTransform.localEulerAngles;

            var iconFrames = hud.itemSlotIconFrames.ToList();
            var icons = hud.itemSlotIcons.ToList();

            // find index of last slot named `slot\d` regex
            var index = iconFrames.Count;

            Plugin.ExtendedLogging("Adding 1 item slots! Surely this will go well..");
            Plugin.ExtendedLogging($"Adding after index: {index}");

            for (int i = 0; i < 1; i++)
            {
                // calculate xPosition to center belt using TotalWidth
                var anchor = -(referenceFrame.rectTransform.parent.GetComponent<RectTransform>().sizeDelta.x / 2) - (15 / 4);

                var xPosition = anchor + (i * slotSizeX) + (i * 15f);


                var prefab = iconFrames[0];

                var frame = Instantiate(prefab, referenceFrame.transform.parent);
                frame.name = $"Slot{lastInventorySize + i}[ProjectSCPDealDevil]";
                frame.rectTransform.anchoredPosition = new Vector2(xPosition, yPosition);
                frame.rectTransform.eulerAngles = frameAngles;

                var icon = frame.transform.GetChild(0).GetComponent<Image>();
                icon.name = "icon";
                icon.enabled = false;
                icon.rectTransform.eulerAngles = iconAngles;
                // rotate 90 degrees because unity is goofy
                icon.rectTransform.Rotate(new Vector3(0.0f, 0.0f, -90.0f));

                var slotIndex = index + i;

                // insert at index
                iconFrames.Insert(slotIndex, frame);
                icons.Insert(slotIndex, icon);

                slotIndexes.Add(slotIndex);

                // move up in parent to match index
                frame.transform.SetSiblingIndex(slotIndex);

                
            }

            hud.itemSlotIconFrames = iconFrames.ToArray();
            hud.itemSlotIcons = icons.ToArray();

            Plugin.ExtendedLogging("Added 1 item slots!");
        } else {
            var iconFrames = hud.itemSlotIconFrames.ToList();
            var icons = hud.itemSlotIcons.ToList();

            var lastInventorySize = iconFrames.Count;

            var slotsRemoved = 0;
            for (int i = lastInventorySize - 1; i >= 0; i--)
            {

                slotsRemoved++;
                var frame = iconFrames[i];
                iconFrames.RemoveAt(i);
                icons.RemoveAt(i);
                Destroy(frame.gameObject);
                if(slotsRemoved >= 1)
                {
                    break;
                }
            }

            hud.itemSlotIconFrames = iconFrames.ToArray();

            hud.itemSlotIcons = icons.ToArray();

            Plugin.ExtendedLogging($"Removed 1 item slots!");

        }
    }
}
