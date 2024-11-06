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
        
        //var hook = new Hook(AccessTools.Method(typeof(ModelReplacementAPI), nameof(ModelReplacementAPI.RemovePlayerModelReplacement)),
        //                    AccessTools.Method(typeof(ModelReplacementAPICompatibilityChecker), nameof(ModelReplacementAPICompatibilityChecker.ResetCamera)));
        
        Plugin.ExtendedLogging("Delilah is a new model registered!");
        PlayerModelHandler.Instance.ModelReplacement = new ModelReplacementAssets("shockwavegalmodelreplacementassets");
        ModelReplacementAPI.RegisterSuitModelReplacement("Delilah", typeof(ShockwaveGalModel));
    }

    /*public static void ResetCamera(Action<PlayerControllerB> orig, PlayerControllerB player)
    {
        orig(player);
        Plugin.ExtendedLogging(new System.Diagnostics.StackTrace());
        GameObject gameObject = player.gameObject;
        gameObject.transform.Find("ScavengerModel").Find("metarig").Find("CameraContainer")
            .Find("MainCamera")
            .Find("HUDHelmetPosition")
            .localPosition = new Vector3(0.01f, -0.048f, -0.063f);
    }

    [HarmonyPatch(typeof(ModelReplacementAPI), "SetPlayerModelReplacement")]
    [HarmonyPostfix]
    public static void DetectCamera(PlayerControllerB player)
    {
        GameObject playerGameObject = player.gameObject;
        Transform cameraContainer = playerGameObject.transform.Find("ScavengerModel").Find("metarig").Find("CameraContainer");
        if (cameraContainer != null)
        {
            if (playerGameObject.GetComponent<ShockwaveGalModel>())
            {
                cameraContainer
                    .Find("MainCamera")
                    .Find("HUDHelmetPosition")
                    .localPosition = new Vector3(0.01f, -0.058f, -0.063f);
            }
        }
    }*/
}