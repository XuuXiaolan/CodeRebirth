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
            // Handle imported assets
            foreach (string asset in importedAssets)
            {
                HandleAsset(asset, isDeleted: false);
            }

            // Handle deleted assets
            foreach (string asset in deletedAssets)
            {
                HandleAsset(asset, isDeleted: true);
            }

            // Handle moved assets
            for (int i = 0; i < movedAssets.Length; i++)
            {
                string oldAssetPath = movedFromAssetPaths[i];
                string newAssetPath = movedAssets[i];

                // Mark the original bundle as changed due to asset move
                if (File.Exists(oldAssetPath))
                {
                    HandleAsset(oldAssetPath, isDeleted: true);
                }

                // Mark the new bundle as changed due to asset move
                HandleAsset(newAssetPath, isDeleted: false);
            }
        }
        catch (Exception e)
        {
            Debug.LogError($"An error occurred during asset post-processing: {e.Message}\n{e.StackTrace}");
        }
    }

    static void HandleAsset(string assetPath, bool isDeleted)
    {
        // Skip processing if the asset is a shader or shadergraph file
        if (assetPath.EndsWith(".shader", StringComparison.OrdinalIgnoreCase) || assetPath.EndsWith(".shadergraph", StringComparison.OrdinalIgnoreCase))
        {
            Debug.Log($"Ignoring shader or shadergraph file: \"{assetPath}\".");
            return;
        }

        // Skip processing if the asset is marked as deleted and doesn't exist
        if (isDeleted && !File.Exists(assetPath))
        {
            Debug.LogWarning($"Asset \"{assetPath}\" is deleted and does not exist.");
            return;
        }

        try
        {
            string bundle = AssetDatabase.GetImplicitAssetBundleName(assetPath);
            if (!string.IsNullOrEmpty(bundle) && CRBundleWindow.bundles.TryGetValue(bundle, out CRBundleWindow.BundleBuildSettings settings))
            {
                if (CRBundleWindow.logChangedFiles)
                {
                    string action = isDeleted ? "Deleted" : "Changed";
                    Debug.Log($"{action} file: \"{assetPath}\" in bundle: \"{bundle}\"");
                }

                if (!settings.ChangedSinceLastBuild)
                {
                    settings.ChangedSinceLastBuild = true;
                    EditorPrefs.SetBool($"{settings.BundleName}_changed", true);
                    Debug.Log($"Bundle: {settings.BundleName} has unbuilt changes.");
                }
            }
        }
        catch (Exception e)
        {
            Debug.LogError($"Failed to handle asset \"{assetPath}\". Error: {e.Message}");
        }
    }
}