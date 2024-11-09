
using GameNetcodeStuff;
using ModelReplacement;
using UnityEngine;

namespace CodeRebirth.src.Content.PlayerModels;
public class ShockwaveGalModel : BodyReplacementBase
{
    protected override void Awake()
    {
        base.Awake();
        controller.gameplayCamera.transform.position += Vector3.up * 0.5f;
        if (controller == GameNetworkManager.Instance.localPlayerController)
        {
            Plugin.ExtendedLogging("Delilah is a new model registered!");
        }
        else
        {
            Plugin.ExtendedLogging("Delilah is not a new model registered!");
        }
    }

    protected override void OnDestroy()
    {
        base.OnDestroy();
        controller.gameplayCamera.transform.position -= Vector3.up * 0.5f;
        if (GetComponent<PlayerControllerB>() != null)
        {
            Plugin.ExtendedLogging("Delilah is a new model unregistered!");
        }
        else
        {
            Plugin.ExtendedLogging("Delilah is not a new model unregistered!");
        }
    }

    protected override GameObject LoadAssetsAndReturnModel()
    { 
        return PlayerModelHandler.Instance.ModelReplacement.ModelPrefab;
    }
}