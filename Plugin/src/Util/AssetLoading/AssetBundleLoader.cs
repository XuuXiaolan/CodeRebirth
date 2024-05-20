using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using Unity.Netcode;
using UnityEngine;
using NetworkPrefabs = LethalLib.Modules.NetworkPrefabs;
using Utilities = LethalLib.Modules.Utilities;

namespace CodeRebirth.Util.AssetLoading;

public class AssetBundleLoader<T> where T : AssetBundleLoader<T> {
	protected AssetBundle bundle;

	protected AssetBundleLoader(string filePath, bool registerNetworkPrefabs = true, bool fixMixerGroups = true) {
		if (!Plugin.LoadedBundles.TryGetValue(filePath, out bundle)) {
			bundle = AssetBundle.LoadFromFile(
				Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), filePath));
			Plugin.LoadedBundles.Add(filePath, bundle);
			Plugin.Logger.LogDebug(
				$"[AssetBundle Loading] {filePath} contains these objects: {string.Join(",", bundle.GetAllAssetNames())}");
		} else {
			Plugin.Logger.LogDebug($"[AssetBundle Loading] Used cached {filePath}");
		}

		Type type = typeof(T);
		foreach (PropertyInfo property in type.GetProperties()) {
			LoadFromBundleAttribute loadInstruction =
				(LoadFromBundleAttribute)property.GetCustomAttribute(typeof(LoadFromBundleAttribute));
			if (loadInstruction == null) continue;

			property.SetValue(this, LoadAsset(bundle, loadInstruction.BundleFile));
		}

		foreach(GameObject gameObject in bundle.LoadAllAssets<GameObject>()) {
			if(fixMixerGroups)
				Utilities.FixMixerGroups(gameObject);
			if(!registerNetworkPrefabs || gameObject.GetComponent<NetworkObject>() == null) continue;
			NetworkPrefabs.RegisterNetworkPrefab(gameObject);
		}
	}

	UnityEngine.Object LoadAsset(AssetBundle bundle, string path) {
		UnityEngine.Object result = bundle.LoadAsset<UnityEngine.Object>(path);
		if(result == null) throw new ArgumentException(path + " is not valid in the assetbundle!");

		return result;
	}
}
