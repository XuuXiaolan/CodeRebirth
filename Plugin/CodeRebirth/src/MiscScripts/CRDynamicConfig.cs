using System;
using System.Collections.Generic;
using UnityEngine;

namespace CodeRebirth.src.MiscScripts;
public enum CRDynamicConfigType
{
    STRING, 
    INT, 
    FLOAT, 
    BOOL 
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

    // Returns a boxed value based on the selected type.
    public BoxedValueSetting GetBoxedValue()
    {
        switch (DynamicConfigType)
        {
            case CRDynamicConfigType.STRING:
                return new BoxedValueSetting(defaultString, Key);
            case CRDynamicConfigType.INT:
                return new BoxedValueSetting(defaultInt, Key);
            case CRDynamicConfigType.FLOAT:
                return new BoxedValueSetting(defaultFloat, Key);
            case CRDynamicConfigType.BOOL:
                return new BoxedValueSetting(defaultBool, Key);
            default:
                throw new NotImplementedException();
        }
    }
}

[CreateAssetMenu(fileName = "DynamicConfigSettings", menuName = "Settings/Dynamic Config Settings")]
public class DynamicConfigSettings : ScriptableObject
{
    public List<CRDynamicConfig> Configs;
}
