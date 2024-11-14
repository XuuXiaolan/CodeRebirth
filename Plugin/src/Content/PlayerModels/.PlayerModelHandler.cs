
using CodeRebirth.src.ModCompats;
using CodeRebirth.src.Util;
using CodeRebirth.src.Util.AssetLoading;
using UnityEngine;

namespace CodeRebirth.src.Content.PlayerModels;
public class PlayerModelHandler : ContentHandler<PlayerModelHandler>
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

    internal SeamineModelReplacementAssets SeamineModelReplacement { get; set; } = null!;
    internal ShockwaveModelReplacementAssets ShockwaveModelReplacement { get; set; } = null!;

    public PlayerModelHandler()
    {
        if (ModelReplacementAPICompatibilityChecker.Enabled)
        {
            ModelReplacementAPICompatibilityChecker.Init();
        }
    }
}