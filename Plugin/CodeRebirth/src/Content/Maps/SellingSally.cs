using System.Collections;
using System.Collections.Generic;
using System.Linq;
using CodeRebirth.src.Content.Items;
using CodeRebirth.src.Util;
using GameNetcodeStuff;
using Unity.Netcode;
using Unity.Netcode.Components;
using UnityEngine;

namespace CodeRebirth.src.Content.Maps;
public class SellingSally : NetworkBehaviour
{
    [Header("Sounds")]
    [SerializeField]
    private AudioSource buttonSource = null!;
    [SerializeField]
    private AudioClip buttonSound = null!;
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
    private Transform endOfBarrelTransform = null!;
    [SerializeField]
    private Transform sallyLoaderTransform = null!;

    private List<GrabbableObject> _sellableScraps = new();
    private static readonly int OpenedAnimation = Animator.StringToHash("open"); // Bool
    private static readonly int ShootAnimation = Animator.StringToHash("shoot"); // Trigger
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
        if (!player.IsOwner) return;
        PressButtonServerRpc();
    }

    [ServerRpc(RequireOwnership = false)]
    public void PressButtonServerRpc()
    {
        if (!CanCurrentlyShoot()) return;
        PlayButtonSoundServerRpc();
        sallyNetworkAnimator.SetTrigger(ShootAnimation);
    }

    public void OnBellInteract(PlayerControllerB player)
    {
        if (!player.IsOwner) return;
        RingBellServerRpc();
    }

    [ServerRpc(RequireOwnership = false)]
    private void PlayButtonSoundServerRpc()
    {
        PlayButtonSoundClientRpc();
    }

    [ClientRpc]
    private void PlayButtonSoundClientRpc()
    {
        buttonSource.PlayOneShot(buttonSound);
    }

    [ServerRpc(RequireOwnership = false)]
    public void RingBellServerRpc()
    {
        sallyAnimator.SetBool(OpenedAnimation, !sallyAnimator.GetBool(OpenedAnimation));
        AnimateBellClientRpc();
    }

    [ClientRpc]
    public void AnimateBellClientRpc()
    {
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
        foreach (var grabbableObject in transform.GetComponentsInChildren<GrabbableObject>())
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
        return true;
    }

    public void DoShootScrapAnimEvent()
    {
        foreach (var player in StartOfRound.Instance.allPlayerScripts)
        {
            if (!player.IsOwner || player.transform.parent != endOfBarrelTransform.parent)
                continue;

            player.transform.position = endOfBarrelTransform.position;
            player.DamagePlayer(9999, true, true, CauseOfDeath.Blast, 0, false, endOfBarrelTransform.forward * 100f);
        }
        int scrapValueToMake = 0;
        CanCurrentlyShoot();
        foreach (var sellableScrap in _sellableScraps)
        {
            scrapValueToMake += sellableScrap.scrapValue;
        }

        HUDManager.Instance.ShakeCamera(ScreenShakeType.VeryStrong);
        SellAndDisplayItemProfits(scrapValueToMake, CodeRebirthUtils.Instance.shipTerminal);
    }

    private void SellAndDisplayItemProfits(int profit, Terminal terminal)
    {
        profit *= 3;
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
}