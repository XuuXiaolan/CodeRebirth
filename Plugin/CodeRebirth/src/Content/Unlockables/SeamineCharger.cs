using Unity.Netcode;

namespace CodeRebirth.src.Content.Unlockables;
public class SeamineCharger : Charger
{
    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();
        if (!IsServer) return;
        // Instantiate the ShockwaveGalAI prefab
        GalAI = Instantiate(UnlockableHandler.Instance.SeamineTink.SeamineGalPrefab, ChargeTransform.position, ChargeTransform.rotation).GetComponent<SeamineGalAI>();
        NetworkObject netObj = GalAI.GetComponent<NetworkObject>();
        GalAI.GalCharger = this;
        netObj.Spawn();
    }
}