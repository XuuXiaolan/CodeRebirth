using BepInEx.Bootstrap;
using CullFactory.Behaviours.API;
using System;
using System.Runtime.CompilerServices;
using UnityEngine;

namespace CodeRebirth.src.ModCompats;
internal static class CullFactorySoftCompat
{
    private static bool CullFactoryDynamicObjectsAPIExists = Chainloader.PluginInfos.TryGetValue("com.fumiko.CullFactory", out var info) && info.Metadata.Version >= new Version(1, 5, 0);

    [MethodImpl(MethodImplOptions.NoInlining | MethodImplOptions.NoOptimization)]
    public static void TryRefreshDynamicLight(Light light)
    {
        RefreshDynamicLight(light);
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    internal static void RefreshDynamicLight(Light light)
    {
        if (CullFactoryDynamicObjectsAPIExists)
        {
            DynamicObjectsAPI.RefreshLightPosition(light);
        }
    }
}