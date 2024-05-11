using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Unity.Netcode;

namespace CodeRebirth.WeatherStuff;

public class MeteorShower : MonoBehaviour
{
    [SerializeField] private int minTimeBetweenSpawns;
    [SerializeField] private int maxTimeBetweenSpawns;
    [SerializeField] private int maxToSpawn;
    private HashSet<GameObject> nodesAlreadyVisited = new HashSet<GameObject>();
    private GameObject[] possibleLandNodes;
    private System.Random random;
    private float lastTimeUsed;
    private float currentTimeOffset;
    private int randomInt;
    private bool canStart = false;
    private const int RandomSeedOffset = -53;

    private void OnEnable()
    {
        InitializeMeteorShower();
        if (IsServerOrHost())
        {
            StartCoroutine(StartCooldown());
            lastTimeUsed = TimeOfDay.Instance.globalTime;
            currentTimeOffset = random.Next(minTimeBetweenSpawns, maxTimeBetweenSpawns); // Initial random delay
            TimeOfDay.Instance.onTimeSync.AddListener(OnGlobalTimeSync);
        }
    }
    private IEnumerator StartCooldown() {
        yield return new WaitForSeconds(5f);
        canStart = true;
    } 
    private void OnDisable()
    {
        if (IsServerOrHost())
        {
            TimeOfDay.Instance.onTimeSync.RemoveListener(OnGlobalTimeSync);
        }
    }

    private void InitializeMeteorShower()
    {
        random = new System.Random(StartOfRound.Instance.randomMapSeed + RandomSeedOffset);
        possibleLandNodes = FetchAndFilterLandNodes();
    }

    private bool IsServerOrHost()
    {
        return NetworkManager.Singleton.IsServer || NetworkManager.Singleton.IsHost;
    }

    private GameObject[] FetchAndFilterLandNodes()
    {
        var nodes = RoundManager.Instance.outsideAINodes;
        return CullNodesByProximity(nodes, 5.0f, true).ToArray();
    }

    private IEnumerable<GameObject> CullNodesByProximity(GameObject[] nodes, float minDistance, bool cullDoors = false)
    {
        var nodeList = new List<GameObject>(nodes);
        var toCull = new HashSet<GameObject>();

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
            nodeList.RemoveAll(n => entrances.Any(e => Vector3.Distance(n.transform.position, e.transform.position) < minDistance));
        }

        return nodeList;
    }

    private void OnGlobalTimeSync()
    {
        float currentTime = TimeOfDay.Instance.globalTime;
        if (currentTime > lastTimeUsed + currentTimeOffset && canStart)
        {
            lastTimeUsed = currentTime;
            currentTimeOffset = random.Next(minTimeBetweenSpawns, maxTimeBetweenSpawns); // Reset delay for next spawn
            StartCoroutine(PlanMeteorEvent());
        }
    }

    private IEnumerator PlanMeteorEvent()
    {
        int meteorsToSpawn = random.Next(1, maxToSpawn + 1);
        for (int i = 0; i < meteorsToSpawn; i++)
        {
            StartCoroutine(PlanMeteorSpawn());
        }
        yield return null; // Ensure the loop completes even if no meteors spawn
    }

    private IEnumerator PlanMeteorSpawn(int maxAttempts = 4)
    {
        bool success = false;
        for (int attempt = 0; attempt < maxAttempts && !success; attempt++)
        {
            success = TrySpawnMeteor();
            yield return null; // Yield return to ensure frame skipping for other processes
        }
    }

    private bool TrySpawnMeteor()
    {
        Vector3 spawnLocation = CalculateSpawnLocation(out GameObject landNode);
        if (nodesAlreadyVisited.Contains(landNode))
        {
            randomInt = -1;
        } else {
            nodesAlreadyVisited.Add(landNode);
            randomInt = random.Next(-1, 100) + 1; // Adjusted to ensure randomness is applied correctly.
        }

        GameObject meteor = Instantiate(Plugin.Meteor, spawnLocation, Quaternion.identity, Plugin.meteorShower.effectObject.transform);
        meteor.GetComponent<NetworkObject>().Spawn();

        // Ensure parameters are set right after spawning and before any updates occur.
        Meteors meteorComponent = meteor.GetComponent<Meteors>();
        if (meteorComponent != null)
        {
            meteorComponent.SetParams(spawnLocation, landNode.transform.position, randomInt);
        }

        return true;
    } //todo, fix meteors spawning at the very start of the weather that dont move.

    private Vector3 CalculateSpawnLocation(out GameObject landNode)
    {
        float spawnRadius = random.Next(540, 2000);
        float heightVariation = random.Next(500, 800);
        var spawnDirection = random.NextDouble() * 2 * Mathf.PI;
        Vector2 meteorSpawnDirection = new Vector2(Mathf.Sin((float)spawnDirection), Mathf.Cos((float)spawnDirection));
        Vector3 meteorSpawnLocationOffset = new Vector3(meteorSpawnDirection.x * spawnRadius, heightVariation, meteorSpawnDirection.y * spawnRadius);

        landNode = possibleLandNodes[random.Next(possibleLandNodes.Length)];
        return landNode.transform.position + meteorSpawnLocationOffset;
    }
}
