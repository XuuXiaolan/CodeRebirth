using System.Collections.Generic;
using System.Linq;
using CodeRebirth.src.Content.Items;
using CodeRebirth.src.MiscScripts;
using Dawn.Utils;
using CodeRebirth.src.Util;
using GameNetcodeStuff;
using Unity.Netcode;
using UnityEngine;
using System;
using System.Collections;
using CodeRebirth.src.Content.Enemies;
using Dusk;

namespace CodeRebirth.src.Content.Maps;
public class ItemCrate : CRHittable
{

    [Header("Hover Tooltips")]
    public string keyHoverTip = "Open : [LMB]";

    [Header("Audio")]
    public AudioSource? slowlyOpeningSFX = null;
    public AudioSource openSFX = null!;

    public InteractTrigger? trigger = null!;
    public Pickable? pickable = null!;
    public Animator animator = null!;
    private bool opened = false;
    private float digProgress = 0;
    public int health = 4;
    public Vector3 originalPosition;
    public System.Random crateRandom = new();
    public static List<Item> ShopItemList = new();
    public AudioClip creepyWarningSound = null!;
    public enum CrateType
    {
        Wooden,
        Metal,
        WoodenMimic,
        MetalMimic,
    }
    public CrateType crateType;
    public Collider mainCollider = null!;
    public GrabAndPullPlayer? grabAndPullPlayerScript = null;
    public GrabAndLaunchPlayer? grabAndLaunchPlayerScript = null;

    private bool openedOnce = false;

    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();
        Transporter.objectsToTransport.Add(gameObject);
    }

    public override void OnNetworkDespawn()
    {
        base.OnNetworkDespawn();
        Transporter.objectsToTransport.Remove(gameObject);
    }

    private void Start()
    {
        if (grabAndPullPlayerScript != null) grabAndPullPlayerScript.enabled = false;
        if (grabAndLaunchPlayerScript != null) grabAndLaunchPlayerScript.enabled = false;
        crateRandom = new System.Random(StartOfRound.Instance.randomMapSeed + CodeRebirthUtils.Instance.CRRandom.Next(100000));
        digProgress = crateRandom.NextFloat(0.01f, 0.1f);
        transform.position = transform.position + transform.up * -0.6f;
        originalPosition = transform.position;
        UpdateDigPosition(0, digProgress);

        if (crateType == CrateType.Wooden || crateType == CrateType.WoodenMimic)
        {
            var healthBoundedRange = MapObjectHandler.Instance.Crate.GetConfig<BoundedRange>("Wooden Crate | Health").Value;
            health = crateRandom.Next((int)healthBoundedRange.Min, (int)healthBoundedRange.Max + 1);
        }

        if ((crateType == CrateType.Metal || crateType == CrateType.MetalMimic) && trigger != null)
        {
            var holdTimerBoundedRange = MapObjectHandler.Instance.Crate.GetConfig<BoundedRange>("Metal Safe | Hold Timer").Value;
            trigger.timeToHold = crateRandom.NextFloat(holdTimerBoundedRange.Min, holdTimerBoundedRange.Max);
            animator.SetFloat("openingSpeed", 11.875f / trigger.timeToHold);
            Plugin.ExtendedLogging("Crate time to hold: " + trigger.timeToHold);
        }

        if ((crateType == CrateType.Wooden || crateType == CrateType.WoodenMimic) && ShopItemList.Count == 0)
        {
            Terminal terminal = CodeRebirthUtils.Instance.shipTerminal;

            foreach (Item item in StartOfRound.Instance.allItemsList.itemsList)
            {
                if (!item.isScrap && terminal.buyableItemsList.Contains(item))
                {
                    ShopItemList.Add(item);
                }
            }
        }
    }

    private void UpdateDigPosition(float old, float newValue)
    {
        if (newValue == 0) originalPosition = transform.position - (transform.up * 0.5f);
        transform.position = originalPosition + (transform.up * newValue * 0.5f);
        Plugin.ExtendedLogging($"ItemCrate was hit! New digProgress: {newValue}");
    }

    private void Update()
    {
        if ((crateType != CrateType.Metal && crateType != CrateType.MetalMimic) || trigger == null || pickable == null) return;
        bool dugOut = digProgress >= 1;
        trigger.interactable = dugOut && !pickable.IsLocked && !opened;
        pickable.enabled = dugOut && pickable.IsLocked && !opened;
    }

    public void OnInteractEarly()
    {
        OnInteractEarlyServerRpc();
    }

    [ServerRpc(RequireOwnership = false)]
    private void OnInteractEarlyServerRpc()
    {
        OnInteractEarlyClientRpc();
    }

    [ClientRpc]
    private void OnInteractEarlyClientRpc()
    {
        slowlyOpeningSFX?.Play();
        animator.SetBool("opening", true);
    }

    public void OnInteract(PlayerControllerB player)
    {
        if (opened)
            return;

        OpenCrateServerRpc();
    }

    public void OnInteractCancel()
    {
        OnInteractCancelServerRpc();
    }

    [ServerRpc(RequireOwnership = false)]
    private void OnInteractCancelServerRpc()
    {
        OnInteractCancelClientRpc();
    }

    [ClientRpc]
    private void OnInteractCancelClientRpc()
    {
        slowlyOpeningSFX?.Stop();
        animator.SetBool("opening", false);
    }

    [ServerRpc(RequireOwnership = false)]
    public void OpenCrateServerRpc()
    {
        OpenCrate();
    }

    public void OpenCrate()
    {
        if (!openedOnce && crateType != CrateType.MetalMimic && crateType != CrateType.WoodenMimic)
        {
            int numberOfScrapToSpawn = 3;
            if (crateType == CrateType.Metal)
            {
                var boundedRange = MapObjectHandler.Instance.Crate.GetConfig<BoundedRange>("Metal Safe | Scrap Spawn Number").Value;
                numberOfScrapToSpawn = UnityEngine.Random.Range((int)boundedRange.Min, (int)boundedRange.Max + 1);
            }
            else if (crateType == CrateType.Wooden)
            {
                var boundedRange = MapObjectHandler.Instance.Crate.GetConfig<BoundedRange>("Wooden Crate | Scrap Spawn Number").Value;
                numberOfScrapToSpawn = UnityEngine.Random.Range((int)boundedRange.Min, (int)boundedRange.Max + 1);
            }

            for (int i = 0; i < numberOfScrapToSpawn; i++)
            {
                SpawnableItemWithRarity chosenItemWithRarity;
                Item? item = null;

                switch (crateType)
                {
                    case CrateType.Metal:
                        string potentiallyBlacklistedScrapConfig = MapObjectHandler.Instance.Crate.GetConfig<string>("Metal Safe | Blacklist").Value;
                        bool actuallyABlacklist = MapObjectHandler.Instance.Crate.GetConfig<bool>("Metal Safe | Blacklist Or Whitelist").Value;

                        string[] blacklistedOrWhitelistedScrap = potentiallyBlacklistedScrapConfig.Split(',').Select(s => s.Trim().ToLowerInvariant()).ToArray();
                        List<SpawnableItemWithRarity> acceptableItems = new();
                        foreach (SpawnableItemWithRarity spawnableItemWithRarity in RoundManager.Instance.currentLevel.spawnableScrap)
                        {
                            if (spawnableItemWithRarity.rarity <= 0)
                                continue;

                            Plugin.ExtendedLogging("Moon's item pool: " + spawnableItemWithRarity.spawnableItem.itemName);
                            if (actuallyABlacklist && !blacklistedOrWhitelistedScrap.Contains(spawnableItemWithRarity.spawnableItem.itemName.ToLowerInvariant()))
                            {
                                acceptableItems.Add(spawnableItemWithRarity);
                            }
                            else if (!actuallyABlacklist && blacklistedOrWhitelistedScrap.Contains(spawnableItemWithRarity.spawnableItem.itemName.ToLowerInvariant()))
                            {
                                acceptableItems.Add(spawnableItemWithRarity);
                            }
                        }
                        chosenItemWithRarity = crateRandom.NextItem(acceptableItems);
                        item = chosenItemWithRarity.spawnableItem;
                        break;
                    case CrateType.Wooden:
                        item = GetRandomShopItem();
                        break;
                }

                if (item == null || item.spawnPrefab == null) continue;
                CodeRebirthUtils.Instance.SpawnScrap(item, transform.position + Vector3.up + Vector3.right * crateRandom.NextFloat(-0.25f, 0.25f) + Vector3.forward * crateRandom.NextFloat(-0.25f, 0.25f), false, true, 0);
            }
        }
        OpenCrateClientRpc();
    }

    [ClientRpc]
    private void OpenCrateClientRpc()
    {
        OpenCrateLocally();
    }

    public void OpenCrateLocally()
    {
        slowlyOpeningSFX?.Stop();
        if (pickable != null && trigger != null)
        {
            pickable.IsLocked = false;
            trigger.enabled = false;
        }
        openSFX.Play();
        mainCollider.enabled = false;
        opened = true;
        openedOnce = true;

        if (crateType == CrateType.Metal || crateType == CrateType.MetalMimic)
        {
            animator.SetBool("opened", true);
        }
        else
        {
            animator.SetBool("opened", true);
        }
        animator.SetBool("opening", false);
        if (grabAndPullPlayerScript != null) grabAndPullPlayerScript.enabled = true;
        if (grabAndLaunchPlayerScript != null) grabAndLaunchPlayerScript.enabled = true;
        if (crateType == CrateType.WoodenMimic)
        {
            StartCoroutine(ResetCrateManually());
        }
    }

    private IEnumerator ResetCrateManually()
    {
        yield return new WaitForSeconds(2f);
        if (health > 0 && digProgress == 0) yield break;
        if (grabAndLaunchPlayerScript != null) grabAndLaunchPlayerScript.enabled = false;
        ResetWoodenCrate();
    }

    [ServerRpc(RequireOwnership = false)]
    private void SetNewDigProgressServerRpc(float newDigProgress)
    {
        SetNewDigProgressClientRpc(newDigProgress);
    }

    [ClientRpc]
    private void SetNewDigProgressClientRpc(float newDigProgress)
    {
        if (Vector3.Distance(transform.position, originalPosition + (transform.up * newDigProgress * 0.5f)) > 0.5f)
        {
            digProgress = 1f;
            return;
        }
        UpdateDigPosition(digProgress, newDigProgress);
        digProgress = Mathf.Clamp01(newDigProgress);
    }

    [ServerRpc(RequireOwnership = false)]
    private void DamageCrateServerRpc(int damage)
    {
        DamageCrateClientRpc(damage);
        if (health - damage == -1)
        {
            OpenCrate();
        }
    }

    [ClientRpc]
    private void DamageCrateClientRpc(int damage)
    {
        health -= damage;
        Plugin.ExtendedLogging("Crate health: " + health);
        if (health <= 1)
        {
            if (crateType != CrateType.MetalMimic && crateType != CrateType.WoodenMimic) return;
            openSFX.PlayOneShot(creepyWarningSound, 1f);
        }
    }

    public override bool Hit(int force, Vector3 hitDirection, PlayerControllerB? playerWhoHit = null, bool playHitSFX = false, int hitID = -1)
    {
        if (opened || playerWhoHit == null) return false;

        bool shovelOnly = false;
        if (crateType == CrateType.Metal || crateType == CrateType.MetalMimic)
        {
            shovelOnly = MapObjectHandler.Instance.Crate.GetConfig<bool>("Metal Safe | Shovellin").Value;
        }
        else if (crateType == CrateType.Wooden || crateType == CrateType.WoodenMimic)
        {
            shovelOnly = MapObjectHandler.Instance.Crate.GetConfig<bool>("Wooden Crate | Shovellin").Value;
        }

        if (playerWhoHit.currentlyHeldObjectServer == null && shovelOnly)
            return false;

        if (digProgress < 1)
        {
            float progressChange = crateRandom.NextFloat(0.15f, 0.25f);
            SetNewDigProgressServerRpc(digProgress + progressChange);
        }
        else if (crateType == CrateType.Wooden || crateType == CrateType.WoodenMimic)
        {
            DamageCrateServerRpc(1);
        }

        // If damage should apply and meets specific conditions, apply player damage
        if (force == 22 || (playerWhoHit != null && playerWhoHit.currentlyHeldObjectServer == null))
        {
            playerWhoHit.DamagePlayer(5, true, true, CauseOfDeath.Crushing, 0, false, default);
        }

        return true;
    }

    public Item GetRandomShopItem()
    {
        string woodenCrateItemConfig = MapObjectHandler.Instance.Crate.GetConfig<string>("Wooden Crate | Blacklist").Value;
        bool isWhitelist = !MapObjectHandler.Instance.Crate.GetConfig<bool>("Wooden Crate | Blacklist Or Whitelist").Value;
        string[] blackListedScrap = [];
        string[] whiteListedScrap = [];
        List<Item> acceptableItems = [];
        if (!isWhitelist)
        {
            blackListedScrap = woodenCrateItemConfig.Split(',').Select(s => s.Trim().ToLowerInvariant()).ToArray();
            foreach (Item item in ShopItemList)
            {
                Plugin.ExtendedLogging("Shop item: " + item.itemName);
                if (!blackListedScrap.Contains(item.itemName.ToLowerInvariant()))
                {
                    acceptableItems.Add(item);
                }
            }
        }
        else
        {
            if (string.IsNullOrEmpty(woodenCrateItemConfig))
            {
                // generate a whitelist and set it to the config
                MapObjectHandler.Instance.Crate.GetConfig<string>("Wooden Crate | Blacklist").Value = GenerateWhiteList();
                woodenCrateItemConfig = MapObjectHandler.Instance.Crate.GetConfig<string>("Wooden Crate | Blacklist").Value;
            }

            whiteListedScrap = woodenCrateItemConfig.Split(',').Select(s => s.Trim().ToLowerInvariant()).ToArray();
            foreach (Item item in ShopItemList)
            {
                if (!whiteListedScrap.Contains(item.itemName.ToLowerInvariant())) continue;
                acceptableItems.Add(item);
            }
            foreach (Item item in StartOfRound.Instance.allItemsList.itemsList)
            {
                if (!whiteListedScrap.Contains(item.itemName.ToLowerInvariant())) continue;
                acceptableItems.Add(item);
            }
        }
        if (acceptableItems.Count <= 0)
        {
            Plugin.Logger.LogError("Acceptable items count is 0, check your wooden crate config to make sure its setup right.");
            return StartOfRound.Instance.allItemsList.itemsList[UnityEngine.Random.Range(0, StartOfRound.Instance.allItemsList.itemsList.Count)];
        }
        return acceptableItems[UnityEngine.Random.Range(0, acceptableItems.Count)];
    }

    public string GenerateWhiteList()
    {
        List<string> whiteListedScrap = new();
        foreach (Item item in ShopItemList)
        {
            whiteListedScrap.Add(item.itemName.ToLowerInvariant());
        }
        return string.Join(",", whiteListedScrap);
    }

    public void OnTriggerEnter(Collider other)
    {
        if (opened && other.TryGetComponent(out PlayerControllerB player) && player.IsLocalPlayer())
        {
            DuskModContent.Achievements.TryTriggerAchievement(CodeRebirthAchievementKeys.SafeAndSound);
            opened = false;
            CloseCrateOnPlayerServerRpc(Array.IndexOf(StartOfRound.Instance.allPlayerScripts, player));
        }
    }

    [ServerRpc(RequireOwnership = false)]
    private void CloseCrateOnPlayerServerRpc(int playerIndex)
    {
        CloseCrateOnPlayerClientRpc(playerIndex);
    }

    [ClientRpc]
    private void CloseCrateOnPlayerClientRpc(int playerIndex)
    {
        CloseCrateOnPlayerLocally(playerIndex);
    }

    public void CloseCrateOnPlayerLocally(int playerIndex)
    {
        PlayerControllerB player = StartOfRound.Instance.allPlayerScripts[playerIndex];
        if (!player.IsLocalPlayer())
        {
            if (pickable != null && trigger != null)
            {
                pickable.IsLocked = true;
                trigger.enabled = true;
            }
            mainCollider.enabled = true;
        }
        openedOnce = true;
        opened = false;
        animator.SetBool("opened", false);
        if (crateType == CrateType.MetalMimic)
        {
            StartCoroutine(DisableGrabPullThing(player));
            StartCoroutine(StartDamagingPlayer(player));
        }
        if (crateType == CrateType.WoodenMimic)
        {
            StartCoroutine(DisableGrabLaunchThing());
        }
    }

    private IEnumerator DisableGrabLaunchThing()
    {
        if (grabAndLaunchPlayerScript != null)
        {
            grabAndLaunchPlayerScript.enabled = true;
            yield return new WaitForSeconds(1.4f);
            grabAndLaunchPlayerScript.enabled = false;
        }
    }

    public void ResetWoodenCrate()
    {
        mainCollider.enabled = true;
        opened = false;
        health = 4;
        animator.SetBool("opened", false);
        UpdateDigPosition(digProgress, 0);
        digProgress = Mathf.Clamp01(0);
    }

    private IEnumerator DisableGrabPullThing(PlayerControllerB player)
    {
        yield return new WaitForSeconds(1.5f);
        if (grabAndPullPlayerScript != null)
        {
            grabAndPullPlayerScript.enabled = false;
            player.Crouch(true);
            player.transform.position = grabAndPullPlayerScript.pullTransform.position;
        }
    }

    public void PlayCreepySoundAnimEvent()
    {
        if (crateType != CrateType.MetalMimic && crateType != CrateType.WoodenMimic) return;
        openSFX.PlayOneShot(creepyWarningSound);
    }

    private IEnumerator StartDamagingPlayer(PlayerControllerB player)
    {
        yield return new WaitForSeconds(1.6f);
        bool trueing = true;
        while (trueing)
        {
            yield return new WaitForSeconds(4f);
            if (player.isPlayerDead || Vector3.Distance(transform.position, player.transform.position) > 3f) trueing = false;
            player.DamagePlayer(10, false, true, CauseOfDeath.Suffocation, 0, false, default);
            player.Crouch(true);
        }
    }
}