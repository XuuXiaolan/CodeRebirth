using System.Collections.Generic;
using System.Linq;
using CodeRebirth.src.Util.Extensions;
using Unity.Netcode;
using UnityEngine;
using Random = System.Random;

namespace CodeRebirth.src.Content.Weathers;
public class CodeRebirthWeathers : MonoBehaviour {

    public Vector3 CalculateAverageLandNodePosition(List<GameObject> nodes)
    {
        Vector3 sumPosition = Vector3.zero;
        int count = 0;

        foreach (GameObject node in nodes)
        {
            sumPosition += node.transform.position;
            count++;
        }

        return count > 0 ? sumPosition / count : Vector3.zero;
    }
	
	public IEnumerable<GameObject> CullNodesByProximity(List<GameObject> nodes, float minDistance = 5f, bool cullDoors = true, bool cullShip = false)
	{
		var nodeList = new List<GameObject>(nodes);
		var toCull = new HashSet<GameObject>();
        Transform shipBoundaries = StartOfRound.Instance.shipBounds.transform;

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
		nodeList.RemoveAll(n => toCull.Contains(n));

		if (cullDoors)
		{
			var entrances = FindObjectsOfType<EntranceTeleport>().ToList();
			nodeList.RemoveAll(n => entrances.Exists(e => Vector3.Distance(n.transform.position, e.transform.position) < minDistance));
		}
		
		if (cullShip) {
			nodeList.RemoveAll(n => Vector3.Distance(n.transform.position, shipBoundaries.position) < 20);
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

	public Vector3 GetRandomTargetPosition(Random random, List<GameObject> nodes, List<GameObject> alreadyUsedNodes, float minX, float maxX, float minY, float maxY, float minZ, float maxZ, float radius) {
		if (StartOfRound.Instance.inShipPhase) return Vector3.zero;
		if (nodes.Count == 0) return Vector3.zero;
		if (maxX < minX) maxX = minX;
		if (maxY < minY) maxY = minY;
		if (maxZ < minZ) maxZ = minZ;
		var nextNode = random.NextItem(nodes);
		Vector3 position = nextNode.transform.position;
		if (!alreadyUsedNodes.Contains(nextNode)) {
			alreadyUsedNodes.Add(nextNode);
		}
		position += new Vector3(random.NextFloat(minX, maxX), random.NextFloat(minY, maxY), random.NextFloat(minZ, maxZ));
		position = RoundManager.Instance.GetRandomNavMeshPositionInBoxPredictable(pos: position, radius: radius, randomSeed: random);
		if (!Plugin.ModConfig.ConfigMeteorHitShip.Value && Vector3.Distance(position, StartOfRound.Instance.shipBounds.transform.position) <= 16) {
			for (int i = 0; i < 5; i++) {
				position = nextNode.transform.position;
				position += new Vector3(random.NextFloat(minX, maxX), random.NextFloat(minY, maxY), random.NextFloat(minZ, maxZ));
				position = RoundManager.Instance.GetRandomNavMeshPositionInBoxPredictable(pos: position, radius: radius, randomSeed: random);
				Plugin.Logger.LogDebug("Selecting random position failed. Trying again.");
				if (Vector3.Distance(position, StartOfRound.Instance.shipBounds.transform.position) > 16) {
					return position;
				}
				else if (i == 4) {
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