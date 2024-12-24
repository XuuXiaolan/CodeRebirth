using System;
using System.Runtime.CompilerServices;
using CodeRebirth.src.Util.AssetLoading;
using CodeRebirthMRAPI.Models;
using ModelReplacement;
using UnityEngine;

namespace CodeRebirthMRAPI;

static class PlayerModelAssets
{
	internal class ShockwaveModelReplacementAssets(string bundleName) : AssetBundleLoader<ShockwaveModelReplacementAssets>(bundleName)
	{
		[LoadFromBundle("ShockwaveGalPlayerModel.prefab")]
		public GameObject ShockwaveModelPrefab { get; private set; } = null!;
	}

	internal class SeamineModelReplacementAssets(string bundleName) : AssetBundleLoader<SeamineModelReplacementAssets>(bundleName)
	{
		[LoadFromBundle("SeamineGalPlayerModel.prefab")]
		public GameObject SeamineModelPrefab { get; private set; } = null!;
	}

	internal class ZortModelReplacementAssets(string bundleName) : AssetBundleLoader<ZortModelReplacementAssets>(bundleName)
	{
		[LoadFromBundle("ZortPlayerModel.prefab")]
		public GameObject ZortModelPrefab { get; private set; } = null!;
	}

	internal static ShockwaveModelReplacementAssets ShockwaveModelAssets;
	internal static SeamineModelReplacementAssets SeamineModelAssets;
	internal static ZortModelReplacementAssets ZortModelAssets;

	[MethodImpl(MethodImplOptions.NoInlining | MethodImplOptions.NoOptimization)]
	internal static void RegisterSuits()
	{
		if (CodeRebirth.src.Plugin.ModConfig.ConfigShockwaveGalPlayerModelEnabled.Value)
		{
			Plugin.ExtendedLogging("Delilah is a new model registered!");
			GrabbableObjectPatches.Init();
			ShockwaveModelAssets = new ShockwaveModelReplacementAssets("shockwavegalmodelreplacementassets");
			ModelReplacementAPI.RegisterSuitModelReplacement("Delilah", typeof(ShockwaveGalModel));
		}

		if (CodeRebirth.src.Plugin.ModConfig.ConfigSeamineTinkPlayerModelEnabled.Value)
		{
			Plugin.ExtendedLogging("Seamine is a new model registered!");
			SeamineModelAssets = new SeamineModelReplacementAssets("seaminegalmodelreplacementassets");
			ModelReplacementAPI.RegisterSuitModelReplacement("Betty", typeof(SeamineGalModel));
		}

		if (true)
		{
			Plugin.ExtendedLogging("Zort is a new model registered!");
			ZortModelAssets = new ZortModelReplacementAssets("zortmodelreplacementassets");
			ModelReplacementAPI.RegisterSuitModelReplacement("Zort", typeof(ZortModel));
		}
	}
}