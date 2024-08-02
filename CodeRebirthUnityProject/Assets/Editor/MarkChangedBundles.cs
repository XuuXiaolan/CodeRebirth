using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;

public class MarkChangedBundles : AssetPostprocessor {
    static void OnPostprocessAllAssets(string[] importedAssets, string[] deletedAssets, string[] movedAssets, string[] movedFromAssetPaths) {
        List<string> allAssets = new List<string>(importedAssets);
        allAssets.AddRange(deletedAssets);

        foreach (string asset in allAssets) {
            if (Path.GetExtension(asset) == "shader" || Path.GetExtension(asset) == "shadergraph") {
                continue;
            }
            string bundle = AssetDatabase.GetImplicitAssetBundleName(asset);
            if (!string.IsNullOrEmpty(bundle) && CRBundeWindow.bundles.TryGetValue(bundle, out CRBundeWindow.BundleBuildSettings settings)) {
                string name = Path.GetFileName(asset);
                if (CRBundeWindow.ignorelist.Split(",").Contains(name)) { // i couldn't care less
                    continue;
                }

                if (CRBundeWindow.logChangedFiles) {
                    Debug.Log($"changed file: \"{name}\"");
                }  

                if(settings.changedSinceLastBuild) continue;
                Debug.Log($"Bundle: {settings.BundleName} has unbuilt changes.");
                settings.changedSinceLastBuild = true;
                EditorPrefs.SetBool($"{settings.BundleName}_changed", true);
            }
        }
    }
}
