using System;
using System.Collections;
using System.Linq;
using UnityEngine;
using Unity.Netcode;
using Unity.Collections;
using GameNetcodeStuff;
using System.Collections.Generic;

namespace CodeRebirth.WeatherStuff;

public class MeteorShower : MonoBehaviour
{
    #pragma warning disable CS0649
    [SerializeField] private LayerMask layersToIgnore;
    [SerializeField] private int minTimeBetweenSpawns;
    [SerializeField] private int maxTimeBetweenSpawns;
    [SerializeField] private int maxToSpawn;
    [SerializeField] private int meteorLandRadius;
    private GameObject[] possibleLandNodes;
    private Vector2 meteorSpawnDirection;
    private Vector3 meteorSpawnLocationOffset;
    private float lastTimeUsed;
    private float currentTimeOffset;
    private System.Random random;
    private const int RandomSeedOffset = -53;

    private void OnEnable()
    {
        if (!(NetworkManager.Singleton.IsServer || NetworkManager.Singleton.IsHost)) return;
        random = new System.Random(StartOfRound.Instance.randomMapSeed + RandomSeedOffset);
        TimeOfDay.Instance.onTimeSync.AddListener(OnGlobalTimeSync);
        // Wait 12-18 seconds before spawning first batch.
        currentTimeOffset = random.Next(12, 18);
        possibleLandNodes = RoundManager.Instance.outsideAINodes;
        possibleLandNodes = CullNearbyNodes(possibleLandNodes, 5.0f);
        possibleLandNodes = CullDoorNodes(possibleLandNodes, 5.0f);
        StartCoroutine(DecideSpawnArea());
    }

    private IEnumerator DecideSpawnArea()
    {
        StartCoroutine(PlanMeteor(4, false, result =>
        {
            if (!result)
            {
                StartCoroutine(DecideSpawnArea());
            }
        }));
        yield return null;
    }

    private void OnDisable()
    {
        TimeOfDay.Instance.onTimeSync.RemoveListener(OnGlobalTimeSync);
    }

    private void OnGlobalTimeSync()
    {
        var time = TimeOfDay.Instance.globalTime;
        if (time <= lastTimeUsed + currentTimeOffset)
            return;
        lastTimeUsed = time;
        PlanStrikes();
    }

    private void PlanStrikes()
    {
        if (!RoundManager.Instance.dungeonFinishedGeneratingForAllPlayers) return;
        currentTimeOffset = random.Next(minTimeBetweenSpawns, maxTimeBetweenSpawns);

        var amountToSpawn = random.Next(1, maxToSpawn);
        
        for (var i = 0; i < amountToSpawn; i++)
        {
            Plugin.Logger.LogInfo(amountToSpawn.ToString());
            Plugin.Logger.LogInfo("starting plan meteor");
            StartCoroutine(PlanMeteor());
        }
    }
    public GameObject[] CullNearbyNodes(GameObject[] nodes, float minDistance)
    {
        // Convert array to list for easier manipulation
        List<GameObject> nodeList = new List<GameObject>(nodes);

        // Use a list to keep track of indices to remove
        List<int> indicesToRemove = new List<int>();

        for (int i = 0; i < nodeList.Count; i++)
        {
            for (int j = i + 1; j < nodeList.Count; j++)
            {
                // Check the distance between nodes[i] and nodes[j]
                if (Vector3.Distance(nodeList[i].transform.position, nodeList[j].transform.position) < minDistance)
                {
                    // Add the index of one of the nodes to the removal list
                    // Here, we choose to add the second node (j) to avoid duplicate removals
                    if (!indicesToRemove.Contains(j)) {
                        indicesToRemove.Add(j);
                    }
                }
            }
        }

        // Sort indices in descending order to avoid shifting indices during removals
        indicesToRemove.Sort((a, b) => b.CompareTo(a));

        // Iterate through the sorted list of indices and remove them from the nodeList
        foreach (int indexToRemove in indicesToRemove)
        {
            nodeList.RemoveAt(indexToRemove);
        }

        // Convert the list back to an array and return
        return nodeList.ToArray();
    }
    public GameObject[] CullDoorNodes(GameObject[] nodes, float minDistance)
    {
        // Get all EntranceTeleport objects that are marked as entrances to buildings
        EntranceTeleport[] entrances = FindObjectsOfType<EntranceTeleport>()
                                        .Where(t => t.isEntranceToBuilding)
                                        .ToArray();

        List<GameObject> nodeList = new List<GameObject>(nodes);

        // List to hold indices of nodes to remove
        List<int> indicesToRemove = new List<int>();

        // Check each node against each entrance
        for (int i = 0; i < nodeList.Count; i++)
        {
            foreach (EntranceTeleport entrance in entrances)
            {
                if (Vector3.Distance(nodeList[i].transform.position, entrance.transform.position) < minDistance)
                {
                    // If the node is within minDistance, mark it for removal and break out of the inner loop
                    indicesToRemove.Add(i);
                    break;
                }
            }
        }

        // Sort indices in descending order to remove from list without affecting earlier indices
        indicesToRemove = indicesToRemove.Distinct().ToList();
        indicesToRemove.Sort((a, b) => b.CompareTo(a));

        // Remove nodes by indices from the list
        foreach (int index in indicesToRemove)
        {
            if (index < nodeList.Count) {
                nodeList.RemoveAt(index);
            }
        }

        return nodeList.ToArray();
    }
    private IEnumerator PlanMeteor(int maxAttempts = 4, bool spawn = true, Action<bool> callback = null)
    {
        Plugin.Logger.LogInfo("Starting PlanMeteor coroutine.");
        bool result = false;
        for (var i = 0; i < maxAttempts; i++)
        {
            Plugin.Logger.LogInfo($"Attempt {i+1}/{maxAttempts}");

            if (!spawn) 
            {
                result = true;
                break;
            }
            // Re-calculate spawn radius and height variation every attempt to ensure randomness
            float spawnRadius = random.Next(540, 2000); // Larger and more variable radius
            float heightVariation = random.Next(500, 800); // Random height for each meteor
            var spawnDirection = (float)random.NextDouble() * 2 * Mathf.PI;
            meteorSpawnDirection = new Vector2(Mathf.Sin(spawnDirection), Mathf.Cos(spawnDirection));
            meteorSpawnLocationOffset = new Vector3(meteorSpawnDirection.x * spawnRadius, heightVariation, meteorSpawnDirection.y * spawnRadius);

            // Determine spawn location
            var landLocation = possibleLandNodes[random.Next(0, possibleLandNodes.Length)].transform.position;
            var spawnLocation = landLocation + meteorSpawnLocationOffset;

            var meteorInstance = Instantiate(Plugin.Meteor, spawnLocation, Quaternion.identity, Plugin.meteorShower.effectObject.transform);
            meteorInstance.GetComponent<NetworkObject>().Spawn(true);
            yield return new WaitUntil(() => meteorInstance.GetComponent<Meteors>().IsSpawned);
            meteorInstance.GetComponent<Meteors>().SetParams(spawnLocation, landLocation);
            Plugin.Logger.LogInfo($"Spawning meteor at {spawnLocation} and landing at {landLocation}");

            result = true;
            break;
        }
        callback?.Invoke(result);
    }

    [Serializable]
    public struct MeteorSpawnInfo
    {
        public double timeToSpawnAt;
        public Vector3 spawnLocation;
        public Vector3 landLocation;

        public MeteorSpawnInfo(double timeToSpawnAt, Vector3 spawnLocation, Vector3 landLocation)
        {
            this.timeToSpawnAt = timeToSpawnAt;
            this.spawnLocation = spawnLocation;
            this.landLocation = landLocation;
        }
    }
}