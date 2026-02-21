using System.Collections;
using System.Collections.Generic;
using System.Linq;
using CodeRebirth.src.Util;
using DunGen;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.AI;

namespace CodeRebirth.src.Content.Enemies;
public class TrashCan : NetworkBehaviour
{
    [SerializeField]
    private MeshFilter? meshFilter = null;

    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();
        Janitor.trashCans.Add(this);
        if (Plugin.ModConfig.ConfigDisableTrashCans.Value && meshFilter != null)
        {
            meshFilter.gameObject.SetActive(false);
        }
    }

    private void Start()
    {
        if (!IsServer)
        {
            return;
        }

        if (Vector3.Distance(this.transform.position, RoundManager.FindMainEntrancePosition(true, false)) <= 5f)
        {
            NetworkObject.Despawn(true);
        }

        List<Tile> allDeadendTiles = RoundManager.Instance.dungeonGenerator.Generator.CurrentDungeon.AllTiles.Where(x => x.UsedDoorways.Count == 1).ToList();

        if (allDeadendTiles.Count > 0)
        {
            Tile tile = allDeadendTiles[UnityEngine.Random.Range(0, allDeadendTiles.Count)];
            Vector3 roomCenter = tile.Bounds.center;
            if (Physics.Raycast(roomCenter, Vector3.down, out RaycastHit raycastHit, 5f, StartOfRound.Instance.collidersAndRoomMaskAndDefault, QueryTriggerInteraction.Ignore))
            {
                roomCenter = raycastHit.point;
            }
            NavMeshHit navMeshHit = default;
            Vector3 randomNavMeshPositionInBoxPredictable = RoundManager.Instance.GetRandomNavMeshPositionInRadius(roomCenter, 6f, navMeshHit);
            transform.position = randomNavMeshPositionInBoxPredictable;
            SyncPositionRpc(randomNavMeshPositionInBoxPredictable);
            Plugin.ExtendedLogging($"Moved trash can to {randomNavMeshPositionInBoxPredictable}");
        }
    }

    [Rpc(SendTo.NotMe)]
    public void SyncPositionRpc(Vector3 position)
    {
        transform.position = position;
    }

    public override void OnNetworkDespawn()
    {
        base.OnNetworkDespawn();
        Janitor.trashCans.Remove(this);
    }
}