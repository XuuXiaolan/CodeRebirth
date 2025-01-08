using Unity.Netcode;

namespace CodeRebirth.src.Content.Enemies;
public class TrashCan : NetworkBehaviour
{
    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();
        Janitor.trashCans.Add(this);
    }

    public override void OnNetworkDespawn()
    {
        base.OnNetworkDespawn();
        Janitor.trashCans.Remove(this);
    }
}