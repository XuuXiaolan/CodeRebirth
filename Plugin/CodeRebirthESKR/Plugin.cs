using BepInEx;
using BepInEx.Logging;
using HarmonyLib;

namespace CodeRebirthESKR;

[BepInPlugin(MyPluginInfo.PLUGIN_GUID, MyPluginInfo.PLUGIN_NAME, MyPluginInfo.PLUGIN_VERSION)]
[BepInDependency(CodeRebirth.MyPluginInfo.PLUGIN_GUID)]
public class Plugin : BaseUnityPlugin {
	public static Plugin Instance { get; private set; } = null!;
	internal new static ManualLogSource Logger { get; private set; } = null!;

	private void Awake() {
		Logger = base.Logger;
		Instance = this;


		Logger.LogInfo($"{MyPluginInfo.PLUGIN_GUID} v{MyPluginInfo.PLUGIN_VERSION} has loaded! :3");
	}
	
	internal static void ExtendedLogging(object text)
	{
		if (CodeRebirth.src.Plugin.ModConfig.ConfigExtendedLogging.Value)
		{
			Logger.LogInfo(text);
		}
	}
}