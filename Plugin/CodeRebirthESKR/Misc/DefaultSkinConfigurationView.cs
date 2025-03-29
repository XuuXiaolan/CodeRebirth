using System;

namespace CodeRebirthESKR.Misc;
[Serializable]
public struct DefaultSkinConfigurationView
{
    public string skinId;
    public DefaultMoonFrequencyView[] defaultEntries;
    public float defaultFrequency;
    public float vanillaFallbackFrequency;
}

[Serializable]
public struct DefaultMoonFrequencyView
{
    public string moonId;
    public float frequency;
}