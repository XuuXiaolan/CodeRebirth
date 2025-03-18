using System;
using UnityEngine;

namespace CodeRebirth.src.MiscScripts;
public struct BoxedValueSetting
{
    public string ValueName { get; private set; }
    public enum SettingType { Int, Float, Bool, String, Enum, Object, None }
    public SettingType ValueType { get; private set; }
    
    private int intValue;
    private float floatValue;
    private bool boolValue;
    private string stringValue;
    private object objectValue;
    private Enum enumValue;

    // Default constructor.
    public BoxedValueSetting(SettingType setting)
    {
        ValueType = SettingType.None;
        ValueName = string.Empty;
        objectValue = null;
        intValue = -1;
        floatValue = -1;
        boolValue = false;
        stringValue = string.Empty;
        enumValue = null;
    }

    public BoxedValueSetting(object newObjectValue, string newValueName = "UNKNOWN")
    {
        ValueName = newValueName;
        ValueType = SettingType.Object;
        objectValue = newObjectValue;
        intValue = -1;
        floatValue = -1;
        boolValue = false;
        stringValue = string.Empty;
        enumValue = null;
    }

    public BoxedValueSetting(float newFloatValue, string newValueName = "UNKNOWN")
    {
        ValueName = newValueName;
        ValueType = SettingType.Float;
        floatValue = newFloatValue;
        intValue = -1;
        objectValue = null;
        boolValue = false;
        stringValue = string.Empty;
        enumValue = null;
    }

    public BoxedValueSetting(string newStringValue, string newValueName = "UNKNOWN")
    {
        ValueName = newValueName;
        ValueType = SettingType.String;
        stringValue = newStringValue;
        intValue = -1;
        floatValue = -1;
        objectValue = null;
        boolValue = false;
        enumValue = null;
    }

    public BoxedValueSetting(Enum newEnumValue, string newValueName = "UNKNOWN")
    {
        ValueName = newValueName;
        ValueType = SettingType.Enum;
        enumValue = newEnumValue;
        intValue = -1;
        floatValue = -1;
        objectValue = null;
        boolValue = false;
        stringValue = string.Empty;
    }

    // New constructors for int and bool.
    public BoxedValueSetting(int newIntValue, string newValueName = "UNKNOWN")
    {
        ValueName = newValueName;
        ValueType = SettingType.Int;
        intValue = newIntValue;
        floatValue = -1;
        objectValue = null;
        boolValue = false;
        stringValue = string.Empty;
        enumValue = null;
    }

    public BoxedValueSetting(bool newBoolValue, string newValueName = "UNKNOWN")
    {
        ValueName = newValueName;
        ValueType = SettingType.Bool;
        boolValue = newBoolValue;
        intValue = -1;
        floatValue = -1;
        stringValue = string.Empty;
        objectValue = null;
        enumValue = null;
    }

    // Getters.
    public bool TryGetBoolValue(out bool returnBoolValue)
    {
        returnBoolValue = false;
        if (ValueType == SettingType.Bool)
        {
            returnBoolValue = boolValue;
            return true;
        }
        return false;
    }

    public bool TryGetIntValue(out int returnIntValue)
    {
        returnIntValue = -1;
        if (ValueType == SettingType.Int)
        {
            returnIntValue = intValue;
            return true;
        }
        return false;
    }

    public bool TryGetFloatValue(out float returnFloatValue)
    {
        returnFloatValue = -1;
        if (ValueType == SettingType.Float)
        {
            returnFloatValue = floatValue;
            return true;
        }
        return false;
    }

    public bool TryGetStringValue(out string returnStringValue)
    {
        returnStringValue = string.Empty;
        if (ValueType == SettingType.String)
        {
            returnStringValue = stringValue;
            return true;
        }
        return false;
    }

    public bool TryGetEnumValue(out Enum returnEnumValue)
    {
        returnEnumValue = null;
        if (ValueType == SettingType.Enum)
        {
            returnEnumValue = enumValue;
            return true;
        }
        return false;
    }

    public bool TryGetObjectValue(out object returnObjectValue)
    {
        returnObjectValue = null;
        if (ValueType == SettingType.Object)
        {
            returnObjectValue = objectValue;
            return true;
        }
        return false;
    }
}
