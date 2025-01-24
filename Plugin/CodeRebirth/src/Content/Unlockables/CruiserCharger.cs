using Unity.Netcode;
/*
namespace CodeRebirth.src.Content.Unlockables;
public class CruiserCharger : Charger
{
    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();
        if (!IsServer) return;
        // Instantiate the CruiserGalAI prefab
        GalAI = Instantiate(UnlockableHandler.Instance.CruiserBot.CruiserDronePrefab, ChargeTransform.position, ChargeTransform.rotation).GetComponent<CruiserGalAI>();
        NetworkObject netObj = GalAI.GetComponent<NetworkObject>();
        GalAI.GalCharger = this;
        netObj.Spawn();
    }
}*/