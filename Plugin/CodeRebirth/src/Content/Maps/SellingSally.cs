using System.Collections;
using System.Collections.Generic;
using CodeRebirth.src.MiscScripts;
using CodeRebirth.src.ModCompats;
using CodeRebirth.src.Util;
using Dawn;
using Dusk;
using Dawn.Utils;

using GameNetcodeStuff;
using Unity.Netcode;
using Unity.Netcode.Components;
using UnityEngine;

namespace CodeRebirth.src.Content.Maps;

public class SellingSally : NetworkBehaviour
{
    [Header("Sounds")]
    [SerializeField]
    private AudioSource sallyElevatorSource = null!;
    [SerializeField]
    private AudioClip elevatorOpenSound = null!;
    [SerializeField]
    private AudioClip elevatorCloseSound = null!;

    [SerializeField]
    private AudioSource sallySource = null!;
    [SerializeField]
    private AudioClip warningSound = null!;

    [SerializeField]
    private AudioSource buttonSource = null!;
    [SerializeField]
    private AudioClip buttonSound = null!;
    [SerializeField]
    private AudioClip errorSound = null!;

    [SerializeField]
    private AudioSource bellSource = null!;
    [SerializeField]
    private AudioClip bellSound = null!;

    [Header("Animations")]
    [SerializeField]
    private Animator sallyAnimator = null!;
    [SerializeField]
    private SkinnedMeshRenderer bellRenderer = null!;
    [SerializeField]
    private NetworkAnimator sallyNetworkAnimator = null!;

    [Header("Misc")]
    [SerializeField]
    private JimothyNPC? _jimothy = null;
    [SerializeField]
    private Transform endOfBarrelTransform = null!;
    [SerializeField]
    private Transform sallyLoaderTransform = null!;
    [SerializeField]
    private Collider sallyLargePlatformCollider = null!;

    private List<GrabbableObject> _sellableScraps = new();
    private static readonly int OpenedAnimation = Animator.StringToHash("open"); // Bool
    private static readonly int ShootAnimation = Animator.StringToHash("shoot"); // Trigger
    private bool _usedOnce = false;
    [HideInInspector] public static SellingSally? Instance = null;

    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();
        Instance = this;
    }

    public override void OnNetworkDespawn()
    {
        base.OnNetworkDespawn();
        Instance = null;
    }

    public void OnButtonInteract(PlayerControllerB player)
    {
        if (!player.IsLocalPlayer()) return;
        PressButtonServerRpc();
    }

    [ServerRpc(RequireOwnership = false)]
    public void PressButtonServerRpc()
    {
        if (!CanCurrentlyShoot())
        {
            PlayErrorSoundClientRpc();
            return;
        }
        PlayButtonSoundServerRpc();
        sallyNetworkAnimator.SetTrigger(ShootAnimation);
    }

    public void OnBellInteract(PlayerControllerB player)
    {
        if (!player.IsLocalPlayer()) return;
        RingBellServerRpc();
    }

    [ServerRpc(RequireOwnership = false)]
    private void PlayButtonSoundServerRpc()
    {
        _jimothy?.PlayerUsedSallyButton();
        PlayButtonSoundClientRpc();
    }

    [ClientRpc]
    private void PlayButtonSoundClientRpc()
    {
        sallySource.PlayOneShot(warningSound);
        buttonSource.PlayOneShot(buttonSound);
    }

    [ServerRpc(RequireOwnership = false)]
    public void RingBellServerRpc()
    {
        bool openedElevator = sallyAnimator.GetBool(OpenedAnimation);
        sallyAnimator.SetBool(OpenedAnimation, !openedElevator);
        AnimateBellClientRpc(!openedElevator);
    }

    [ClientRpc]
    public void AnimateBellClientRpc(bool openingElevator)
    {
        sallyElevatorSource.PlayOneShot(openingElevator ? elevatorOpenSound : elevatorCloseSound);
        bellSource.PlayOneShot(bellSound);
        StartCoroutine(AnimateBell());
    }

    public IEnumerator AnimateBell()
    {
        float timeElapsed = 0f;
        while (timeElapsed <= 0.25f)
        {
            timeElapsed += Time.deltaTime;
            bellRenderer.SetBlendShapeWeight(0, timeElapsed * 4 * 100);
            yield return null;
        }

        while (timeElapsed > 0)
        {
            timeElapsed -= Time.deltaTime;
            bellRenderer.SetBlendShapeWeight(0, timeElapsed * 4 * 100);
            yield return null;
        }
    }

    private bool CanCurrentlyShoot()
    {
        _sellableScraps.Clear();
        GrabbableObject[] grabbableObjects = transform.GetComponentsInChildren<GrabbableObject>();
        if (grabbableObjects.Length == 0)
            return false;

        foreach (var grabbableObject in grabbableObjects)
        {
            if (grabbableObject.transform.parent != sallyLoaderTransform) continue;
            if (grabbableObject.itemProperties.itemName == "Sally Cube" || grabbableObject.itemProperties.itemName == "Flattened Body")
            {
                _sellableScraps.Add(grabbableObject);
                continue;
            }
            _sellableScraps.Clear();
            return false;
        }
        foreach (var sellableScrap in _sellableScraps)
        {
            sellableScrap.grabbable = false;
        }
        if (_sellableScraps.Count == 0)
            return false;

        return true;
    }

    public void DoShootScrapAnimEvent()
    {
        if (GameNetworkManager.Instance.localPlayerController.transform.parent == endOfBarrelTransform.parent)
        {
            GameNetworkManager.Instance.localPlayerController.transform.position = endOfBarrelTransform.position;
            GameNetworkManager.Instance.localPlayerController.DamagePlayer(9999, true, true, CauseOfDeath.Blast, 0, false, endOfBarrelTransform.forward * 100f);
        }
        else if (sallyLargePlatformCollider.bounds.Contains(GameNetworkManager.Instance.localPlayerController.transform.position))
        {
            GameNetworkManager.Instance.localPlayerController.KillPlayer(GameNetworkManager.Instance.localPlayerController.velocityLastFrame, true, CauseOfDeath.Blast, 9);
        }

        int scrapValueToMake = 0;
        if (!CanCurrentlyShoot())
        {
            PlayErrorSound();
            return;
        }
        foreach (var sellableScrap in _sellableScraps)
        {
            sellableScrap.SetScrapValue(sellableScrap.scrapValue * 3);
            scrapValueToMake += sellableScrap.scrapValue;
        }

        SellAndDisplayItemProfits(scrapValueToMake, CodeRebirthUtils.Instance.shipTerminal);
    }

    private void SellAndDisplayItemProfits(int profit, Terminal terminal)
    {
        TooManyEmotesCompat.AddCredits(profit);
        if (!_usedOnce)
        {
            foreach (var enemyLevelSpawner in EnemyLevelSpawner.enemyLevelSpawners)
            {
                enemyLevelSpawner.spawnTimerMin /= 8f;
                enemyLevelSpawner.spawnTimerMax /= 8f;
            }
            OxydeLightsManager.oxydeLightsManager.IncrementLights();
            _usedOnce = true;
            HUDManager.Instance.DisplayTip("Warning!", "Rampant underground activity detected, evacuation recommended.", true);
        }

        DuskModContent.Achievements.TryTriggerAchievement(CodeRebirthAchievementKeys.Internship);
        if (WeatherRegistry.WeatherManager.GetCurrentLevelWeather().name.ToLowerInvariant().Trim() == "night shift")
        {
            DuskModContent.Achievements.TryTriggerAchievement(CodeRebirthAchievementKeys.GraveyardShift);
        }
        terminal.groupCredits += profit;
        StartOfRound.Instance.gameStats.scrapValueCollected += profit;
        TimeOfDay.Instance.quotaFulfilled += profit;
        HUDManager.Instance.DisplayCreditsEarning(profit, _sellableScraps.ToArray(), terminal.groupCredits);

        foreach (var sellableScrap in _sellableScraps)
        {
            if (IsServer)
                sellableScrap.NetworkObject.Despawn();
        }
        _sellableScraps.Clear();
        TimeOfDay.Instance.UpdateProfitQuotaCurrentTime();
    }

    [ClientRpc]
    public void PlayErrorSoundClientRpc()
    {
        PlayErrorSound();
    }

    public void PlayErrorSound()
    {
        buttonSource.PlayOneShot(errorSound);
    }
}