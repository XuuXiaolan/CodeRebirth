using Unity.Netcode;

namespace CodeRebirth.src.Content.Items;
public class Turbulence : CRWeapon
{
    private bool stuckToGround = false;

    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();
        OnSurfaceHit.AddListener(OnSurfaceHitEvent);
    }

    public void OnSurfaceHitEvent(int surfaceID)
    {
        OnSurfaceHitServerRpc(surfaceID);
    }

    [ServerRpc(RequireOwnership = false)]
    public void OnSurfaceHitServerRpc(int surfaceID)
    {
        OnSurfaceHitClientRpc(surfaceID);
    }

    [ClientRpc]
    public void OnSurfaceHitClientRpc(int surfaceID)
    {
        stuckToGround = true;
        if (playerHeldBy != GameNetworkManager.Instance.localPlayerController)
        {
            return;
        }
        Plugin.ExtendedLogging($"Turbulence hit surface: {surfaceID} so dropping");
        StartCoroutine(playerHeldBy.waitToEndOfFrameToDiscard());
    }

    public override void FallWithCurve()
    {
        if (stuckToGround) return;
        base.FallWithCurve();
    }

    public override void EquipItem()
    {
        base.EquipItem();
        grabbable = true;
        stuckToGround = false;
    }
}