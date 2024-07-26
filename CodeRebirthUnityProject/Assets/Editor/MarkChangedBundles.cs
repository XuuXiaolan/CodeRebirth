using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

public class MarkChangedBundles : AssetPostprocessor {
    static void OnPostprocessAllAssets(string[] importedAssets, string[] deletedAssets, string[] movedAssets, string[] movedFromAssetPaths) {
        List<string> allAssets = new List<string>(importedAssets);
        allAssets.AddRange(deletedAssets);

        foreach (string asset in allAssets) {
            string bundle = AssetDatabase.GetImplicitAssetBundleName(asset);
            if (!string.IsNullOrEmpty(bundle) && CRBundeWindow.bundles.TryGetValue(bundle, out CRBundeWindow.BundleBuildSettings settings)) {
                if(settings.changedSinceLastBuild) continue;
                Debug.Log($"Bundle: {settings.BundleName} has unbuilt changes.");
                settings.changedSinceLastBuild = true;
                EditorPrefs.SetBool($"{settings.BundleName}_changed", true);
            }
        }
    }
}
