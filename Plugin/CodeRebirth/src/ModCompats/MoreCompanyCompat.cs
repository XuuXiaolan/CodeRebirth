using BepInEx.Bootstrap;
using CodeRebirthLib.Utils;
using GameNetcodeStuff;
using MoreCompany.Cosmetics;
using System;
using System.Runtime.CompilerServices;

namespace CodeRebirth.src.ModCompats;
internal static class MoreCompanySoftCompat
{
    private static bool MoreCompanyAPIExists = Chainloader.PluginInfos.TryGetValue("me.swipez.melonloader.morecompany", out var info) && info.Metadata.Version >= new Version(1, 5, 0);

    [MethodImpl(MethodImplOptions.NoInlining | MethodImplOptions.NoOptimization)]
    internal static void TryDisableOrEnableCosmetics(PlayerControllerB targetPlayer, bool disable)
    {
        if (MoreCompanyAPIExists)
        {
            DisableOrEnableCosmetics(targetPlayer, disable);
        }
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static void DisableOrEnableCosmetics(PlayerControllerB targetPlayer, bool disable)
    {
        CosmeticApplication cosmeticApplication = targetPlayer.transform.Find("ScavengerModel").Find("metarig").gameObject.GetComponent<CosmeticApplication>();
        if (disable)
        {
            foreach (var spawnedCosmetic in cosmeticApplication.spawnedCosmetics)
            {
                spawnedCosmetic.gameObject.SetActive(!disable);
            }
        }
        else
        {
            cosmeticApplication.UpdateAllCosmeticVisibilities(targetPlayer.IsLocalPlayer());
        }
    }
}