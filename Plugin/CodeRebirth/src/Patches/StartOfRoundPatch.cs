using HarmonyLib;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.SceneManagement;
using CodeRebirth.src.Util;
using Dawn.Utils;
using System.Diagnostics;
using CodeRebirth.src.Content.Unlockables;
using CodeRebirth.src.Content.Enemies;
using CodeRebirth.src.Content.Maps;
using Dawn;
using UnityEngine.InputSystem.Utilities;
using CodeRebirth.src.ModCompats;

namespace CodeRebirth.src.Patches;
[HarmonyPatch(typeof(StartOfRound))]
static class StartOfRoundPatch
{
    private static bool _patched = false;

    [HarmonyPatch(nameof(StartOfRound.Awake))]
    [HarmonyPostfix]
    public static void StartOfRound_Awake(ref StartOfRound __instance)
    {
        Plugin.ExtendedLogging("StartOfRound.Awake");
        __instance.NetworkObject.OnSpawn(CreateNetworkManager);

        if (!_patched)
        {
            _patched = true;
            if (LateGameUpgradesCompat.LateGameUpgradesExists)
            {
                LateGameUpgradesCompat.PatchDropshipUpgrades();
            }
        }
    }

    private static void CreateNetworkManager()
    {
        if (StartOfRound.Instance.IsServer || StartOfRound.Instance.IsHost)
        {
            if (CodeRebirthUtils.Instance == null)
            {
                GameObject utilsInstance = GameObject.Instantiate(Plugin.Assets.UtilsPrefab);
                SceneManager.MoveGameObjectToScene(utilsInstance, StartOfRound.Instance.gameObject.scene);
                utilsInstance.GetComponent<NetworkObject>().Spawn();
                Plugin.ExtendedLogging($"Created CodeRebirthUtils. Scene is: '{utilsInstance.scene.name}'");
            }
            else
            {
                Plugin.Logger.LogWarning("CodeRebirthUtils already exists?");
            }
        }

        if (EnemyHandler.Instance.DuckSong != null)
        {
            Plugin.ExtendedLogging("Creating duck UI");
            var canvasObject = GameObject.Find("Systems/UI/Canvas");
            var duckUI = GameObject.Instantiate(EnemyHandler.Instance.DuckSong.DuckUIPrefab, Vector3.zero, Quaternion.identity, canvasObject.transform);
        }
    }

    [HarmonyPatch(nameof(StartOfRound.OnShipLandedMiscEvents)), HarmonyPostfix]
    public static void OnShipLandedMiscEventsPatch(StartOfRound __instance)
    {
        foreach (SCP999GalAI gal in SCP999GalAI.Instances)
        {
            gal.MakeTriggerInteractable(true);
        }

        if (Plugin.ModConfig.ConfigRemoveInteriorFog.Value)
        {
            Plugin.ExtendedLogging("Disabling halloween fog");
            if (RoundManager.Instance.indoorFog.gameObject.activeSelf)
            {
                RoundManager.Instance.indoorFog.gameObject.SetActive(false);
            }
        }

        foreach (GalAI gal in GalAI.Instances)
        {
            if (gal.IdleSounds.Length <= 0)
                continue;

            gal.GalVoice.PlayOneShot(gal.IdleSounds[gal.galRandom.Next(gal.IdleSounds.Length)]);
        }
    }
}