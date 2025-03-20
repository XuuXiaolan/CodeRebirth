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
}

[CreateAssetMenu(fileName = "DynamicConfigSettings", menuName = "CodeRebirth/Dynamic Config Settings", order = 2)]
public class DynamicConfigSettings : ScriptableObject
{
    public List<CRDynamicConfig> Configs;
}
