using System.Runtime.CompilerServices;
using CodeRebirth.src.Util.AssetLoading;
using ModelReplacement;
using UnityEngine;

namespace CodeRebirth.src.ModCompats;
public static class ModelReplacementAPICompatibilityChecker
{
    internal class ModelReplacementAssets(string bundleName) : AssetBundleLoader<ModelReplacementAssets>(bundleName)
    {
        [LoadFromBundle("ShockwaveGalPlayerModel.prefab")]
        public GameObject ModelPrefab { get; private set; } = null!;
    }
    internal static ModelReplacementAssets ModelReplacement { get; private set; } = null!;

    public class ShockwaveGalModel : BodyReplacementBase
    {
        protected override GameObject LoadAssetsAndReturnModel()
        { 
            return ModelReplacement.ModelPrefab;
        }
    }

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
        ModelReplacement = new ModelReplacementAssets("shockwavegalmodelreplacementassets");
        ModelReplacementAPI.RegisterSuitModelReplacement("Delilah", typeof(ShockwaveGalModel));
    }
}