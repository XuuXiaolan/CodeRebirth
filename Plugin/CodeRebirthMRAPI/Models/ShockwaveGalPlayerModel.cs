using System.Linq;
using ModelReplacement;
using UnityEngine;

namespace CodeRebirthMRAPI.Models;
public class ShockwaveGalModel : BodyReplacementBase
{
    private Transform controllerLowerSpine = null!;
    protected override GameObject LoadAssetsAndReturnModel()
    { 
        return PlayerModelAssets.ShockwaveModelAssets.ShockwaveModelPrefab;
    }

    protected override void Awake()
    {
        base.Awake();
        Plugin.ExtendedLogging("Setting jetpack stuff");
        controllerLowerSpine = controller.lowerSpine;
        controller.lowerSpine = replacementModel.transform;
    }

    public void OnDisable()
    {
        Plugin.ExtendedLogging("Resetting jetpack stuff");
        controller.lowerSpine = controllerLowerSpine;
    }
}