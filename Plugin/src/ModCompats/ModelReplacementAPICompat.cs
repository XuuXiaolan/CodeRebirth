using System.Runtime.CompilerServices;
using CodeRebirth.src.Content.PlayerModels;
using ModelReplacement;
using static CodeRebirth.src.Content.PlayerModels.PlayerModelHandler;

namespace CodeRebirth.src.ModCompats;
public static class ModelReplacementAPICompatibilityChecker
{
    public static bool Enabled { get { return BepInEx.Bootstrap.Chainloader.PluginInfos.ContainsKey("meow.ModelReplacementAPI"); } }
    
    [MethodImpl(MethodImplOptions.NoInlining | MethodImplOptions.NoOptimization)]
    public static void Init()
    {
        Plugin.ExtendedLogging("No way ModelReplacementApi is on?!");
        Plugin.ModelReplacementAPIIsOn = true;
        if (Plugin.MoreSuitsIsOn) InitialiseImpl();
    }

    [MethodImpl(MethodImplOptions.NoInlining | MethodImplOptions.NoOptimization)]
    private static void InitialiseImpl()
    {
        Plugin.ExtendedLogging("Delilah is a new model registered!");
        PlayerModelHandler.Instance.ModelReplacement = new ModelReplacementAssets("shockwavegalmodelreplacementassets");
        ModelReplacementAPI.RegisterSuitModelReplacement("Delilah", typeof(ShockwaveGalModel));
    }
}