using Unity.Netcode;

namespace CodeRebirth.src.Content.Unlockables;
public class SeamineCharger : Charger
{
    public void Start()
    {
        if (IsServer)
        {
            // Instantiate the ShockwaveGalAI prefab
            GalAI = Instantiate(UnlockableHandler.Instance.SeamineTink.SeamineGalPrefab, ChargeTransform.position, ChargeTransform.rotation, this.transform).GetComponent<SeamineGalAI>();
            NetworkObject netObj = GalAI.GetComponent<NetworkObject>();
            netObj.Spawn();
        }
    }
}