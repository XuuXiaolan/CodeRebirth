using System;
using System.Reflection;
using BepInEx;
using BepInEx.Logging;
using CodeRebirth.src;
using CodeRebirthMRAPI.Models;
using HarmonyLib;
using ModelReplacement;

namespace CodeRebirthMRAPI;

[BepInPlugin(MyPluginInfo.PLUGIN_GUID, MyPluginInfo.PLUGIN_NAME, MyPluginInfo.PLUGIN_VERSION)]
[BepInDependency(CodeRebirth.MyPluginInfo.PLUGIN_GUID)]
[BepInDependency("meow.ModelReplacementAPI", BepInDependency.DependencyFlags.SoftDependency)]
[BepInDependency("x753.More_Suits", BepInDependency.DependencyFlags.SoftDependency)]
public class Plugin : BaseUnityPlugin {
	public static Plugin Instance { get; private set; }
	internal new static ManualLogSource Logger { get; private set; }

	private void Awake() {
		Logger = base.Logger;
		Instance = this;

		if (!BepInEx.Bootstrap.Chainloader.PluginInfos.ContainsKey("meow.ModelReplacementAPI")) {
			Logger.LogInfo("ModelReplacementAPI is not installed, skipping!");
			return;
		}

		if (!BepInEx.Bootstrap.Chainloader.PluginInfos.ContainsKey("x753.More_Suits")) {
			Logger.LogInfo("MoreSuits is not installed, skipping!");
		}
		
		if (CodeRebirth.src.Plugin.ModConfig.ConfigShockwaveGalPlayerModelEnabled.Value)
		{
			ExtendedLogging("Delilah is a new model registered!");
			PlayerModelAssets.ShockwaveModelAssets = new PlayerModelAssets.ShockwaveModelReplacementAssets("shockwavegalmodelreplacementassets");
			ModelReplacementAPI.RegisterSuitModelReplacement("Delilah", typeof(ShockwaveGalModel));
		}

		/*if (Plugin.ModConfig.ConfigSeamineTinkPlayerModelEnabled.Value)
		{
			ExtendedLogging("Seamine is a new model registered!");
			PlayerModelAssets.SeamineModelAssets = new PlayerModelAssets.SeamineModelReplacementAssets("seaminegalmodelreplacementassets");
			ModelReplacementAPI.RegisterSuitModelReplacement("Seamine", typeof(SeamineGalModel));
		}*/
		
		Logger.LogInfo($"{MyPluginInfo.PLUGIN_GUID} v{MyPluginInfo.PLUGIN_VERSION} has loaded! :3");
	}
	
	internal static void ExtendedLogging(object text)
	{
		if (CodeRebirth.src.Plugin.ModConfig.ConfigEnableExtendedLogging.Value)
		{
			Logger.LogInfo(text);
		}
	}
}