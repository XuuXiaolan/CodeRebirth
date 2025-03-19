using System;
using System.Collections.Generic;
using UnityEngine;

namespace CodeRebirth.src.MiscScripts;
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
    public string Key;
    public CRDynamicConfigType DynamicConfigType;

    public string defaultString;
    public int defaultInt;
    public float defaultFloat;
    public bool defaultBool;

    public string Description;
}

[CreateAssetMenu(fileName = "DynamicConfigSettings", menuName = "CodeRebirth/Dynamic Config Settings")]
public class DynamicConfigSettings : ScriptableObject
{
    public List<CRDynamicConfig> Configs;
}
