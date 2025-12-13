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

    private IEnumerator Start()
    {
        yield return new WaitForSeconds(1f);
        List<Tile> allDeadendTiles = RoundManager.Instance.dungeonGenerator.Generator.CurrentDungeon.AllTiles.Where(x => x.UsedDoorways.Count == 1).ToList();

        if (allDeadendTiles.Count > 0)
        {
            Tile tile = allDeadendTiles[CodeRebirthUtils.Instance.CRRandom.Next(allDeadendTiles.Count)];
            NavMeshHit hit = default(NavMeshHit);
            Vector3 randomNavMeshPositionInBoxPredictable = RoundManager.Instance.GetRandomNavMeshPositionInBoxPredictable(tile.Bounds.center, 6f, hit, CodeRebirthUtils.Instance.CRRandom, NavMesh.AllAreas);
            transform.position = randomNavMeshPositionInBoxPredictable;
            // transform.rotation = Quaternion.LookRotation(Vector3.up, hit.normal);
            Plugin.ExtendedLogging($"Moved trash can to {transform.position}");
        }
    }

    public override void OnNetworkDespawn()
    {
        base.OnNetworkDespawn();
        Janitor.trashCans.Remove(this);
    }
}