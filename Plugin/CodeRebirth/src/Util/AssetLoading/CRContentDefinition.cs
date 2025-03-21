using System.Collections.Generic;
using CodeRebirth.src.MiscScripts.ConfigManager;
using UnityEngine;

namespace CodeRebirth.src.Util.AssetLoading;
public class CRContentDefinition : ScriptableObject
{
    public List<CRDynamicConfig> ConfigEntries;
}

public static class CRDynamicConfigExtensions
{
    public static CRDynamicConfig? GetCRDynamicConfigWithSetting(this List<CRDynamicConfig> ConfigEntries, string _settingName, string _settingDesc)
    {
        foreach (var entry in ConfigEntries)
        {
            if (entry.GetCRDynamicConfigWithSetting(_settingName, _settingDesc) != null) return entry;
        }
        return null;
    }
}