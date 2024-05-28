using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using CodeRebirth.Misc;
using Newtonsoft.Json.Serialization;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.AI;
using Random = System.Random;

namespace CodeRebirth.WeatherStuff;

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
	
	public IEnumerable<GameObject> CullNodesByProximity(List<GameObject> nodes, float minDistance = 5f, bool cullDoors = true)
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

		return nodeList;
	}
	public bool IsAuthority()
    {
        return NetworkManager.Singleton.IsHost || NetworkManager.Singleton.IsServer;
    }
}