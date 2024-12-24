using Unity.Netcode;

namespace CodeRebirth.src.Content.Unlockables;
public class TerminalCharger : Charger
{
    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();
        if (!IsServer) return;
        // Instantiate the ShockwaveGalAI prefab
        GalAI = Instantiate(UnlockableHandler.Instance.TerminalBot.TerminalGalPrefab, ChargeTransform.position, ChargeTransform.rotation).GetComponent<TerminalGalAI>();
        NetworkObject netObj = GalAI.GetComponent<NetworkObject>();
        GalAI.GalCharger = this;
        netObj.Spawn();
    }
}