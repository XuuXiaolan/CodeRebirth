using System;
using System.Collections.Generic;
using UnityEngine;

namespace CodeRebirth.src.MiscScripts.ConfigManager;
public enum CRDynamicConfigType
{
    String, 
    Int, 
    Float, 
    Bool 
}

[Serializable]
public class CRDynamicConfig
{
    public string settingName;
    public string settingDesc;
    public CRDynamicConfigType DynamicConfigType;

    public string defaultString;
    public int defaultInt;
    public float defaultFloat;
    public bool defaultBool;

    public string Description;

    public CRDynamicConfig? GetCRDynamicConfigWithSetting(string _settingName, string _settingDesc)
    {
        if (_settingName.ToLowerInvariant().Contains(settingName.ToLowerInvariant()) && _settingDesc.ToLowerInvariant().Contains(settingDesc.ToLowerInvariant())) return this;
        return null;
    }
}