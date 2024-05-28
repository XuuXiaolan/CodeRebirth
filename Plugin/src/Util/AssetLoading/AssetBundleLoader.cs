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
		bool usingCached = Plugin.LoadedBundles.TryGetValue(filePath, out bundle); // fix for registering network objects multiple times, not the best but oh well
		if (!usingCached) {
			bundle = AssetBundle.LoadFromFile(
				Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), filePath));
			Plugin.LoadedBundles.Add(filePath, bundle);
			Plugin.Logger.LogDebug(
				$"[AssetBundle Loading] {filePath} contains these objects: {string.Join(",", bundle.GetAllAssetNames())}");
		} else {
			usingCached = true;
			Plugin.Logger.LogDebug($"[AssetBundle Loading] Used cached {filePath}");
		}

		Type type = typeof(T);
		foreach (PropertyInfo property in type.GetProperties()) {
			LoadFromBundleAttribute loadInstruction =
				(LoadFromBundleAttribute)property.GetCustomAttribute(typeof(LoadFromBundleAttribute));
			if (loadInstruction == null) continue;

			property.SetValue(this, LoadAsset(bundle, loadInstruction.BundleFile));
		}

		if (usingCached) {
			Plugin.Logger.LogDebug("Skipping registering stuff as this bundle has already been loaded");
			return;
		}
		
		foreach(GameObject gameObject in bundle.LoadAllAssets<GameObject>()) {
			if(fixMixerGroups) {
				Utilities.FixMixerGroups(gameObject);
				Plugin.Logger.LogInfo($"[AssetBundle Loading] Fixed Mixer Groups: {gameObject.name}");
			}
			if(!registerNetworkPrefabs || gameObject.GetComponent<NetworkObject>() == null) continue;
			NetworkPrefabs.RegisterNetworkPrefab(gameObject);
			Plugin.Logger.LogInfo($"[AssetBundle Loading] Registered Network Prefab: {gameObject.name}");
		}
		foreach (Item item in bundle.LoadAllAssets<Item>()) {
			if (!registerNetworkPrefabs || item.spawnPrefab.GetComponent<NetworkObject>() == null) continue;
			NetworkPrefabs.RegisterNetworkPrefab(item.spawnPrefab);
			Plugin.Logger.LogInfo($"[AssetBundle Loading] Registered Network Prefab: {item.name}");
		}
	}

	UnityEngine.Object LoadAsset(AssetBundle bundle, string path) {
		UnityEngine.Object result = bundle.LoadAsset<UnityEngine.Object>(path);
		if(result == null) throw new ArgumentException(path + " is not valid in the assetbundle!");

		return result;
	}
}
