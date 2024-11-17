using ModelReplacement;
using UnityEngine;

namespace CodeRebirthMRAPI.Models;
public class ShockwaveGalModel : BodyReplacementBase
{
    protected override GameObject LoadAssetsAndReturnModel()
    { 
        return PlayerModelAssets.ShockwaveModelAssets.ShockwaveModelPrefab;
    }
}