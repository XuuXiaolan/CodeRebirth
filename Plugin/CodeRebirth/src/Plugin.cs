using System.Reflection;
using UnityEngine;
using BepInEx;
using BepInEx.Logging;
using HarmonyLib;
using Dawn.Utils;
using CodeRebirth.src.ModCompats;
using CodeRebirth.src.Patches;
using Unity.Netcode;
using BepInEx.Configuration;
using Dusk;
using Dawn;

namespace CodeRebirth.src;
[BepInPlugin(MyPluginInfo.PLUGIN_GUID, MyPluginInfo.PLUGIN_NAME, MyPluginInfo.PLUGIN_VERSION)]
public class Plugin : BaseUnityPlugin
{
    internal new static ManualLogSource Logger = null!;
    internal static readonly Harmony _harmony = new Harmony(MyPluginInfo.PLUGIN_GUID);
    internal static IngameKeybinds InputActionsInstance = null!;
    public static ConfigFile configFile { get; private set; } = null!;
    public static CodeRebirthConfig ModConfig { get; private set; } = null!; // prevent from accidently overriding the config
    public static DuskMod Mod { get; private set; } = null!;

    internal class MainAssets(AssetBundle bundle) : AssetBundleLoader<MainAssets>(bundle)
    {
        [LoadFromBundle("CodeRebirthUtils.prefab")]
        public GameObject UtilsPrefab { get; private set; } = null!;

        [LoadFromBundle("EmptyNetworkObject.prefab")]
        public GameObject EmptyNetworkObject { get; private set; } = null!;
    }
    internal static MainAssets Assets { get; private set; } = null!;

    internal const ulong GLITCH_STEAM_ID = 9;

    private void Awake()
    {

    }

    internal static void ExtendedLogging(object text)
    {
        if (ModConfig.ConfigExtendedLogging.Value)
        {
            Logger.LogInfo(text);
        }
    }

    private void InitializeNetworkBehaviours()
    {
        var types = Assembly.GetExecutingAssembly().GetLoadableTypes();
        foreach (var type in types)
        {
            if (type.IsNested || !typeof(NetworkBehaviour).IsAssignableFrom(type))
                continue; // we do not care about fixing it, if it is not a network behaviour

            var methods = type.GetMethods(BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static);
            foreach (var method in methods)
            {
                var attributes = method.GetCustomAttributes(typeof(RuntimeInitializeOnLoadMethodAttribute), false);
                if (attributes.Length <= 0)
                    continue;

                method.Invoke(null, null);
            }
        }
    }
}