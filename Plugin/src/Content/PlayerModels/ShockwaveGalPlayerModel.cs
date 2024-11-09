
using ModelReplacement;
using UnityEngine;

namespace CodeRebirth.src.Content.PlayerModels;
public class ShockwaveGalModel : BodyReplacementBase
{
    protected override GameObject LoadAssetsAndReturnModel()
    { 
        return PlayerModelHandler.Instance.ModelReplacement.ModelPrefab;
    }
}