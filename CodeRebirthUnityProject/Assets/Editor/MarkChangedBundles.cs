using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

public class MarkChangedBundles : AssetPostprocessor
{
    static void OnPostprocessAllAssets(string[] importedAssets, string[] deletedAssets, string[] movedAssets, string[] movedFromAssetPaths)
    {
        try
        {
            List<string> allAssets = new List<string>(importedAssets);
            allAssets.AddRange(deletedAssets);
            allAssets.AddRange(movedAssets);

            foreach (string asset in allAssets)
            {
                string extension = Path.GetExtension(asset).ToLower();
                if (extension == ".shader" || extension == ".shadergraph")
                {
                    continue;
                }

                string bundle = AssetDatabase.GetImplicitAssetBundleName(asset);
                if (!string.IsNullOrEmpty(bundle) && CRBundleWindow.bundles.TryGetValue(bundle, out CRBundleWindow.BundleBuildSettings settings))
                {
                    string name = Path.GetFileName(asset);

                    if (CRBundleWindow.logChangedFiles)
                    {
                        Debug.Log($"Changed file: \"{name}\" in bundle: \"{bundle}\"");
                    }

                    if (!settings.ChangedSinceLastBuild)
                    {
                        Debug.Log($"Bundle: {settings.BundleName} has unbuilt changes.");
                        settings.ChangedSinceLastBuild = true;
                        EditorPrefs.SetBool($"{settings.BundleName}_changed", true);
                    }
                }
                else
                {
                    // Log only if the asset belongs to a known bundle but isn't tracked
                    if (!string.IsNullOrEmpty(bundle))
                    {
                        Debug.LogWarning($"Asset \"{asset}\" does not belong to any known bundle or the bundle is not tracked.");
                    }
                }
            }
        }
        catch (Exception e)
        {
            Debug.LogError($"An error occurred during asset post-processing: {e.Message}\n{e.StackTrace}");
        }
    }
}
