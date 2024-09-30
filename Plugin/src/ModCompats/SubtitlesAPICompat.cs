using System.Runtime.CompilerServices;

namespace CodeRebirth.src.ModCompats;
public static class SubtitlesAPICompatibilityChecker
{
    public static bool Enabled { get { return BepInEx.Bootstrap.Chainloader.PluginInfos.ContainsKey("JustJelly.SubtitlesAPI"); } }
    
    [MethodImpl(MethodImplOptions.NoInlining | MethodImplOptions.NoOptimization)]
    public static void Init()
    {
        Plugin.ExtendedLogging("No way subtitlesapi is on?!");
        Plugin.SubtitlesAPIIsOn = true;
        InitSounds();
    }

    [MethodImpl(MethodImplOptions.NoInlining | MethodImplOptions.NoOptimization)]
    private static void InitSounds()
    {
        SubtitlesAPI.SubtitlesAPI.Localization.AddTranslation("WingFlap", "Cutiefly Wing Flap");
    }
} // tbd.