using Unity.Netcode;

namespace CodeRebirth.src.Content.Unlockables;
public class ShockwaveCharger : Charger
{
    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();
        if (!IsServer) return;
        // Instantiate the ShockwaveGalAI prefab
        GalAI = Instantiate(UnlockableHandler.Instance.ShockwaveBot.ShockWaveDronePrefab, ChargeTransform.position, ChargeTransform.rotation).GetComponent<ShockwaveGalAI>();
        NetworkObject netObj = GalAI.GetComponent<NetworkObject>();
        GalAI.GalCharger = this;
        netObj.Spawn();
    }
}