using Unity.Netcode;

namespace CodeRebirth.src.Content.Unlockables;
public class ShockwaveCharger : Charger
{
    public void Start()
    {
        if (IsServer)
        {
            // Instantiate the ShockwaveGalAI prefab
            GalAI = Instantiate(UnlockableHandler.Instance.ShockwaveBot.ShockWaveDronePrefab, ChargeTransform.position, ChargeTransform.rotation, this.transform).GetComponent<ShockwaveGalAI>();
            NetworkObject netObj = GalAI.GetComponent<NetworkObject>();
            netObj.Spawn();
        }
    }
}