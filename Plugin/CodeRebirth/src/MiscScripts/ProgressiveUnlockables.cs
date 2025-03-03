using System.Collections;
using System.Collections.Generic;
using System.Linq;
using CodeRebirth.src.Util;
using UnityEngine;

namespace CodeRebirth.src.MiscScripts;
public class ProgressiveUnlockables
{
    public static Dictionary<UnlockableItem, bool> unlockableIDs = new();
    public static List<string> unlockableNames = new();
    public static List<TerminalNode> rejectionNodes = new();

    public static IEnumerator LoadUnlockedIDs()
    {
        yield return new WaitUntil(() => CodeRebirthUtils.Instance != null);
        for (int i = 0; i < unlockableIDs.Count; i++)
        {
            UnlockableItem unlockable = unlockableIDs.Keys.ElementAt(i);
            bool actuallyUnlocked = ES3.Load(unlockable.ToString(), false, CodeRebirthUtils.Instance.SaveSettings);
            if (Plugin.ModConfig.ConfigUnlockAllGals.Value) actuallyUnlocked = true;
            Plugin.ExtendedLogging($"Unlockable {unlockable.unlockableName} is unlocked: {actuallyUnlocked}");
            unlockableIDs[unlockable] = actuallyUnlocked;
            unlockable.unlockableName = actuallyUnlocked ? unlockableNames[i] : "???";
        }
    }

    public static void SaveUnlockedIDs()
    {
        for (int i = 0; i < unlockableIDs.Count; i++)
        {
            UnlockableItem unlockable = unlockableIDs.Keys.ElementAt(i);
            bool unlocked = unlockableIDs[unlockable];
            ES3.Save(unlockable.ToString(), unlocked, CodeRebirthUtils.Instance.SaveSettings);
            unlockableIDs[unlockable] = unlocked;
            int index = unlockableIDs.Keys.ToList().IndexOf(unlockable);
            unlockable.unlockableName = unlocked ? unlockableNames[index] : "???";
        }
    }
}