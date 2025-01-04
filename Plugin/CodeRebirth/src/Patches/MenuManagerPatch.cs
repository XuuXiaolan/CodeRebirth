using System;
using BepInEx;
using CodeRebirth.src.Util;
using HarmonyLib;
using UnityEngine;

namespace CodeRebirth.src.Patches;
[HarmonyPatch(typeof(MenuManager))]
static class MenuManagerPatch
{
    private static bool alreadyPatched = false;

    [HarmonyPatch(nameof(MenuManager.Start)), HarmonyPostfix] // thanks, funo
    static void MenuManagerStartPatch()
    {
        if (alreadyPatched)
            return;

        var enemyArray = Resources.FindObjectsOfTypeAll<EnemyType>(); // thanks, Zaggy1024
        foreach (EnemyType enemy in enemyArray)
        {
            if (String.IsNullOrEmpty(enemy.enemyName)) continue;
            Plugin.ExtendedLogging($"{enemy.enemyName} has been found!");

            if (!CodeRebirthUtils.EnemyTypes.Contains(enemy)) CodeRebirthUtils.EnemyTypes.Add(enemy);
        }

        alreadyPatched = true; //exiting to the menu launches this too, so we prevent the search just in case
    }
}