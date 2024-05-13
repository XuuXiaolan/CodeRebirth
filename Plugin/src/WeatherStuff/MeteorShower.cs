using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Unity.Netcode;
using CodeRebirth.Misc;

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
        Plugin.Logger.LogInfo("Enabling Meteor Shower");
        InitializeMeteorShower();
        StartCoroutine(StartCooldown());
        lastTimeUsed = TimeOfDay.Instance.globalTime;
        currentTimeOffset = random.Next(minTimeBetweenSpawns, maxTimeBetweenSpawns);
        SpawnInitialMeteorCluster();
        TimeOfDay.Instance.onTimeSync.AddListener(OnGlobalTimeSync);
    }
    private void SpawnInitialMeteorCluster()
    {
        Vector3 averageLocation = CalculateAverageLandNodePosition();
        Vector3 centralLocation = averageLocation + new Vector3(0, random.Next(250, 300), 0);

        GameObject largeMeteor = Instantiate(Plugin.Meteor, centralLocation, Quaternion.identity, Plugin.meteorShower.effectPermanentObject.transform);
        largeMeteor.transform.localScale *= 50; // Scale up for the large central meteor
        DisableParticles(largeMeteor);
        AddRandomMovement(largeMeteor, 3f); // Smaller speed for larger meteor

        int smallMeteorsCount = random.Next(15, 45); // Random number of smaller meteors
        for (int i = 0; i < smallMeteorsCount; i++)
        {
            Vector3 randomOffset = new Vector3(random.Next(-175, 175), random.Next(-50, 50), random.Next(-175, 175));
            GameObject smallMeteor = Instantiate(Plugin.Meteor, centralLocation + randomOffset, Quaternion.identity, Plugin.meteorShower.effectPermanentObject.transform);
            smallMeteor.transform.localScale *= ((float)random.NextDouble() * 6f) + 2f; // Random smaller scale
            DisableParticles(smallMeteor);
            AddRandomMovement(smallMeteor, 4f); // Slightly higher speed for smaller meteors
        }
    }

    private Vector3 CalculateAverageLandNodePosition()
    {
        Vector3 sumPosition = Vector3.zero;
        int count = 0;

        foreach (GameObject node in possibleLandNodes)
        {
            sumPosition += node.transform.position;
            count++;
        }

        if (count > 0)
            return sumPosition / count;
        else
            return Vector3.zero; // Return a default position if no nodes are found
    }

    private void DisableParticles(GameObject meteor)
    {
        meteor.transform.Find("FlameStream").Find("FireEmbers").GetComponent<ParticleSystem>().Stop();
        meteor.transform.Find("FlameStream").Find("FireEmbers").GetComponent<ParticleSystem>().Clear();
        meteor.GetComponentInChildren<ParticleSystem>().Stop();
        meteor.GetComponentInChildren<ParticleSystem>().Clear();
    }

    private void AddRandomMovement(GameObject meteor, float speed)
    {
        var rb = meteor.GetComponent<Rigidbody>();
        if (rb == null)
        {
            rb = meteor.AddComponent<Rigidbody>();
            rb.mass = 1000f; // A high mass to minimize environmental impact while still allowing movement
            rb.useGravity = false; // Ensure the meteor doesn't fall due to gravity
            rb.isKinematic = false; // The Rigidbody will respond to physics but isn't subject to gravity
        }

        // Initial direction setup to mostly avoid downward movement
        Vector3 initialDirection = new Vector3(
            (float)random.NextDouble() * 2 - 1,  // X-axis: Full random
            Mathf.Max(0.5f, (float)random.NextDouble()),  // Y-axis: Strong upward bias
            (float)random.NextDouble() * 2 - 1   // Z-axis: Full random
        );
        rb.velocity = initialDirection.normalized * speed;

        // Limit rotation to Y-axis to minimize influence on velocity direction
        rb.angularVelocity = new Vector3(0, (float)random.NextDouble() * 100 - 50, 0);

        // Continuously adjust direction to ensure stability if necessary
        meteor.AddComponent<StabilizeMovement>().Initialize(rb, initialDirection.normalized * speed);
    }


    private IEnumerator StartCooldown() {
        yield return new WaitForSeconds(5f);
        canStart = true;
    }
    private void OnDisable()
    {
        Plugin.Logger.LogInfo("Disabling Meteor Shower");
        canStart = false;
        nodesAlreadyVisited = new HashSet<GameObject>();
        TimeOfDay.Instance.onTimeSync.RemoveListener(OnGlobalTimeSync);
        if (IsServerOrHost())
        {
            KillAllChildrenClientRpc();
        }
    }
    [ClientRpc]
	public void KillAllChildrenClientRpc() {
        for(int i = Plugin.meteorShower.effectPermanentObject.transform.childCount - 1; i >= 0; i--) {
            GameObject.Destroy(Plugin.meteorShower.effectPermanentObject.transform.GetChild(i).gameObject);
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

        GameObject meteor = Instantiate(Plugin.Meteor, spawnLocation, Quaternion.identity, Plugin.meteorShower.effectPermanentObject.transform);
        if (IsServerOrHost()) {
            meteor.GetComponent<NetworkObject>().Spawn(true);
        }
        // Ensure parameters are set right after spawning and before any updates occur.
        Meteors meteorComponent = meteor.GetComponent<Meteors>();
        if (meteorComponent != null)
        {
            meteorComponent.SetParams(spawnLocation, landNode.transform.position, randomInt);
        }

        return true;
    }

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
public class StabilizeMovement : MonoBehaviour
{
    private Rigidbody rb;
    private Vector3 targetVelocity;

    public void Initialize(Rigidbody rigidbody, Vector3 velocity)
    {
        rb = rigidbody;
        targetVelocity = velocity;
    }

    void FixedUpdate()
    {
        rb.velocity = targetVelocity; // Continuously reset velocity to the intended direction
    }
}