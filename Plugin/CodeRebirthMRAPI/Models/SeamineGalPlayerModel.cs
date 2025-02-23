using ModelReplacement;
using UnityEngine;

namespace CodeRebirthMRAPI.Models;
public class SeamineGalModel : BodyReplacementBase
{
    protected override GameObject LoadAssetsAndReturnModel()
    { 
        return PlayerModelAssets.SeamineModelAssets.SeamineModelPrefab;
    }
}