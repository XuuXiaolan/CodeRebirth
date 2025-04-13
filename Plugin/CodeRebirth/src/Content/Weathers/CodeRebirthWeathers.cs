using System.Collections.Generic;
using System.Linq;
using Unity.Netcode;
using UnityEngine;

namespace CodeRebirth.src.Content.Weathers;
public class CodeRebirthWeathers : MonoBehaviour
{
    public IEnumerable<GameObject> CullNodesByProximity(IEnumerable<GameObject> nodes, float minDistance = 5f, bool cullDoors = true, bool cullShip = false, float shipCullDistance = 20f)
    {
        var nodeList = new List<GameObject>(nodes);
        var toCull = new HashSet<GameObject>();
        Transform shipBoundaries = StartOfRound.Instance.shipLandingPosition;

        // Compare each node with every other node
        for (int i = 0; i < nodeList.Count; i++)
        {

            for (int j = i + 1; j < nodeList.Count; j++)
            {
                if (Vector3.Distance(nodeList[i].transform.position, nodeList[j].transform.position) < minDistance)
                {
                    // Mark the second node in each pair for culling
                    toCull.Add(nodeList[j]);
                }
            }
        }

        // Remove the marked nodes
        nodeList.RemoveAll(toCull.Contains);

        if (cullDoors)
        {
            var entrances = FindObjectsOfType<EntranceTeleport>().ToList();
            nodeList.RemoveAll(n => entrances.Exists(e => Vector3.Distance(n.transform.position, e.transform.position) < minDistance));
        }

        if (cullShip)
        {
            nodeList.RemoveAll(n => Vector3.Distance(n.transform.position, shipBoundaries.position) < shipCullDistance);
        }

        return nodeList;
    }

    public void ChangeCurrentLevelMaximumPower(int outsidePower, int insidePower, int dayTimePower)
    {
        if (!Plugin.ModConfig.ConfigAllowPowerLevelChangesFromWeather.Value) return;
        RoundManager.Instance.currentLevel.maxOutsideEnemyPowerCount = Mathf.Clamp(RoundManager.Instance.currentLevel.maxOutsideEnemyPowerCount + outsidePower, 0, 999);
        RoundManager.Instance.currentLevel.maxEnemyPowerCount = Mathf.Clamp(RoundManager.Instance.currentLevel.maxEnemyPowerCount + insidePower, 0, 999);
        RoundManager.Instance.currentLevel.maxDaytimeEnemyPowerCount = Mathf.Clamp(RoundManager.Instance.currentLevel.maxDaytimeEnemyPowerCount + dayTimePower, 0, 999);
    }

    public Vector3 GetRandomTargetPosition(IEnumerable<GameObject> nodes, List<GameObject> alreadyUsedNodes, float minX, float maxX, float minY, float maxY, float minZ, float maxZ, float radius)
    {
        if (nodes.Count() == 0) return Vector3.zero;
        GameObject? nextNode = nodes.ElementAt(UnityEngine.Random.Range(0, nodes.Count()));
        if (nextNode == null) return Vector3.zero;
        Vector3 position = nextNode.transform.position;
        if (!alreadyUsedNodes.Contains(nextNode))
        {
            alreadyUsedNodes.Add(nextNode);
        }
        position += new Vector3(UnityEngine.Random.Range(minX, maxX), UnityEngine.Random.Range(minY, maxY), UnityEngine.Random.Range(minZ, maxZ));
        position = RoundManager.Instance.GetRandomNavMeshPositionInRadius(position, radius);
        if (!Plugin.ModConfig.ConfigMeteorHitShip.Value && Vector3.Distance(position, StartOfRound.Instance.shipLandingPosition.transform.position) <= 25)
        {
            for (int i = 0; i < 5; i++)
            {
                position = nextNode.transform.position;
                position += new Vector3(UnityEngine.Random.Range(minX, maxX), UnityEngine.Random.Range(minY, maxY), UnityEngine.Random.Range(minZ, maxZ));
                position = RoundManager.Instance.GetRandomNavMeshPositionInRadius(position, radius);
                if (Vector3.Distance(position, StartOfRound.Instance.shipLandingPosition.transform.position) > 25)
                {
                    return position;
                }
                else if (i == 4)
                {
                    Plugin.Logger.LogWarning("Selecting random position failed.");
                    return Vector3.zero;
                }
            }
        }
        return position;
    }

    public bool IsAuthority()
    {
        return NetworkManager.Singleton.IsHost || NetworkManager.Singleton.IsServer;
    }
}