using Unity.Netcode;

namespace CodeRebirth.src.Content.Unlockables;
public class ShockwaveCharger : Charger
{
    public void Start()
    {
        if (IsServer)
        {
            // Instantiate the ShockwaveGalAI prefab
            GalAI = Instantiate(UnlockableHandler.Instance.ShockwaveBot.ShockWaveDronePrefab, ChargeTransform.position, ChargeTransform.rotation).GetComponent<ShockwaveGalAI>();
            NetworkObject netObj = GalAI.GetComponent<NetworkObject>();

            // Spawn the NetworkObject to make it accessible across the network
            netObj.Spawn();

            // Set the correct transform parent and move the instantiated object after it has been spawned
            GalAI.transform.SetParent(this.transform, true);
        }
    }
}