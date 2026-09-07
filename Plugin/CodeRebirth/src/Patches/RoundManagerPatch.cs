using System.Collections.Generic;
using HarmonyLib;
using UnityEngine;
using CodeRebirth.src.Content.Unlockables;
using Dawn;
using Unity.Netcode;

namespace CodeRebirth.src.Patches;

[HarmonyPatch(typeof(RoundManager))]
static class RoundManagerPatch
{
    internal static List<GrabbableObject> plushiesCollectedToday = new();

    internal static readonly NamespacedKey MilitaryAmountKey = NamespacedKey.From("code_rebirth", "military_plane_coverage");

    [HarmonyPatch(nameof(RoundManager.SpawnOutsideHazards)), HarmonyPrefix]
    private static void SpawnOutsideMapObjects()
    {
        if (!NetworkManager.Singleton.IsServer)
        {
            return;
        }

        if (MoneyCounter.Instance == null || MoneyCounter.Instance.MoneyStored() >= 0)
        {
            return;
        }

        PersistentDataContainer contract = DawnLib.GetCurrentContract()!;
        for (int i = 0; i < contract.GetOrSetDefault(MilitaryAmountKey, 1); i++)
        {
            GameObject militaryPlane = GameObject.Instantiate(LethalContent.MapObjects[CodeRebirthMapObjectKeys.MilitaryPlane].GetMapObjectPrefab()!, Vector3.zero, Quaternion.identity, RoundManager.Instance.mapPropsContainer.transform);
            militaryPlane.GetComponent<NetworkObject>().Spawn(true);
        }
    }

    [HarmonyPatch(nameof(RoundManager.UnloadSceneObjectsEarly)), HarmonyPostfix]
    private static void ReturnToOrbitMiscPatch()
    {
        plushiesCollectedToday.Clear();
        foreach (GalAI gal in GalAI.Instances)
        {
            gal.RefillChargesServerRpc();
        }

        foreach (SCP999GalAI gal in SCP999GalAI.Instances)
        {
            gal.MakeTriggerInteractableServerRpc(false);
        }
    }

    [HarmonyPatch(nameof(RoundManager.LoadNewLevelWait))]
    [HarmonyPrefix]
    public static void LoadNewLevelWaitPatch(RoundManager __instance)
    {
        if (!__instance.currentLevel.planetHasTime && TimeOfDay.Instance.daysUntilDeadline == 0)
        {
            if (Plugin.ModConfig.Config999GalCompanyMoonRecharge.Value)
            {
                foreach (SCP999GalAI gal in SCP999GalAI.Instances)
                {
                    gal.RechargeGalHealsAndRevivesServerRpc(true, true);
                }
            }
        }

        if (!Plugin.ModConfig.Config999GalCompanyMoonRecharge.Value)
        {
            foreach (SCP999GalAI gal in SCP999GalAI.Instances)
            {
                gal.RechargeGalHealsAndRevivesServerRpc(true, true);
            }
        }
    }
}