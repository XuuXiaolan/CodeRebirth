using System;
using CodeRebirth.src.Util;
using HarmonyLib;
using UnityEngine;

namespace CodeRebirth.src.Patches;
[HarmonyPatch(typeof(QuickMenuManager))]
static class QuickMenuManagerPatch
{
    private static bool alreadyPatched = false;

    [HarmonyPatch(nameof(QuickMenuManager.Start)), HarmonyPostfix] // thanks, funo
    static void QuickMenuManagerStartPatch()
    {
        if (alreadyPatched)
            return;

        var enemyArray = Resources.FindObjectsOfTypeAll<EnemyType>(); // thanks, Zaggy1024
        foreach (EnemyType enemy in enemyArray)
        {
            if (String.IsNullOrEmpty(enemy.enemyName)) continue;
            Plugin.ExtendedLogging($"{enemy.enemyName} has been found!", (int)Logging_Level.High);

            if (!CodeRebirthUtils.EnemyTypes.Contains(enemy)) CodeRebirthUtils.EnemyTypes.Add(enemy);
        }

        alreadyPatched = true; // exiting to the menu launches this too, so we prevent the search just in case
    }
}