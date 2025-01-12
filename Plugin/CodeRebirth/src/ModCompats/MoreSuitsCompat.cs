using System.Runtime.CompilerServices;

namespace CodeRebirth.src.ModCompats;
public static class MoreSuitsCompatibilityChecker
{
    public static bool Enabled { get { return BepInEx.Bootstrap.Chainloader.PluginInfos.ContainsKey("x753.More_Suits"); } }
    

    [MethodImpl(MethodImplOptions.NoInlining | MethodImplOptions.NoOptimization)]
    public static void Init()
    {
        Plugin.ExtendedLogging("No way More Suits is on?!", (int)Logging_Level.Medium);
        Plugin.MoreSuitsIsOn = true;
    }
}