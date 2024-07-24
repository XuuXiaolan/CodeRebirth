using System.Runtime.CompilerServices;
namespace CodeRebirth.Dependency;

public static class SubtitlesAPICompatibilityChecker {
    public static bool Enabled { get { return BepInEx.Bootstrap.Chainloader.PluginInfos.ContainsKey("JustJelly.SubtitlesAPI"); } }
    
    [MethodImpl(MethodImplOptions.NoInlining | MethodImplOptions.NoOptimization)]
    public static void Init() {
        Plugin.Logger.LogInfo("No way subtitlesapi is on?!");
        Plugin.SubtitlesAPIIsOn = true;
        InitSounds();
    }

    private static void InitSounds() {
        SubtitlesAPI.SubtitlesAPI.Localization.AddTranslation("WingFlap", "Cutiefly Wing Flap");
    }
} // tbd.