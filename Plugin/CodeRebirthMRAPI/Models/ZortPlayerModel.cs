using ModelReplacement;
using UnityEngine;

namespace CodeRebirthMRAPI.Models;
public class ZortModel : BodyReplacementBase
{
    protected override GameObject LoadAssetsAndReturnModel()
    { 
        return PlayerModelAssets.ZortModelAssets.ZortModelPrefab;
    }
}