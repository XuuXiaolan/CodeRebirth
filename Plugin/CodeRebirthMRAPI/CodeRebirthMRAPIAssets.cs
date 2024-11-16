using CodeRebirth.src.Util.AssetLoading;
using UnityEngine;

namespace CodeRebirthMRAPI;

static class CodeRebirthMRAPIAssets {
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

	internal static ShockwaveModelReplacementAssets ShockwaveModelAssets;
	internal static SeamineModelReplacementAssets SeamineModelAssets;
}