
using CodeRebirth.src.ModCompats;
using CodeRebirth.src.Util;
using CodeRebirth.src.Util.AssetLoading;
using UnityEngine;

namespace CodeRebirth.src.Content.PlayerModels;
public class PlayerModelHandler : ContentHandler<PlayerModelHandler>
{
    internal class ModelReplacementAssets(string bundleName) : AssetBundleLoader<ModelReplacementAssets>(bundleName)
    {
        [LoadFromBundle("ShockwaveGalPlayerModel.prefab")]
        public GameObject ModelPrefab { get; private set; } = null!;
    }
    internal ModelReplacementAssets ModelReplacement { get; set; } = null!;

    public PlayerModelHandler()
    {
        if (ModelReplacementAPICompatibilityChecker.Enabled)
        {
            ModelReplacementAPICompatibilityChecker.Init();
        }
    }
}