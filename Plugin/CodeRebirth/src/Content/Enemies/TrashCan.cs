using Unity.Netcode;
using UnityEngine;

namespace CodeRebirth.src.Content.Enemies;
public class TrashCan : NetworkBehaviour
{
    public MeshFilter meshFilter = null!;

    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();
        Janitor.trashCans.Add(this);
        if (Plugin.ModConfig.ConfigDisableTrashCans.Value)
        {
            meshFilter.gameObject.SetActive(false);
        }
    }

    public override void OnNetworkDespawn()
    {
        base.OnNetworkDespawn();
        Janitor.trashCans.Remove(this);
    }
}