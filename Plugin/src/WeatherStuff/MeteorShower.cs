using System;
using System.Collections;
using System.Linq;
using UnityEngine;
using Unity.Netcode;
using Unity.Collections;
using GameNetcodeStuff;

namespace CodeRebirth.WeatherStuff;

public class MeteorShower : MonoBehaviour
{
    [SerializeField] private LayerMask layersToIgnore = 0;
    [SerializeField] private int minTimeBetweenSpawns = 5;
    [SerializeField] private int maxTimeBetweenSpawns = 10;
    [SerializeField] private int maxToSpawn = 50;
    [SerializeField] private int meteorLandRadius = 6;

    private Vector2 meteorSpawnDirection;
    private Vector3 meteorSpawnLocationOffset;

    private float lastTimeUsed;
    private float currentTimeOffset;
    private System.Random random;
    private GameObject meteorPrefab;

    private const int RandomSeedOffset = -53;

    private void OnEnable()
    {
        if (!(NetworkManager.Singleton.IsServer || NetworkManager.Singleton.IsHost)) return;
        meteorPrefab = Plugin.Meteor;
        if (meteorPrefab == null)
        {
            Plugin.Logger.LogInfo("Failed to load meteor prefab.");
            return;
        }
        random = new System.Random(StartOfRound.Instance.randomMapSeed + RandomSeedOffset);
        TimeOfDay.Instance.onTimeSync.AddListener(OnGlobalTimeSync);

        StartCoroutine(DecideSpawnArea());

        // Wait 12-18 seconds before spawning first batch.
        currentTimeOffset = random.Next(12, 18);
    }

    private IEnumerator DecideSpawnArea()
    {
        var spawnDirection = (float)random.NextDouble() * 2 * Mathf.PI;
        meteorSpawnDirection = new Vector2(Mathf.Sin(spawnDirection), Mathf.Cos(spawnDirection));
        meteorSpawnLocationOffset = new Vector3(meteorSpawnDirection.x * random.Next(540, 1200), 350,
        meteorSpawnDirection.y * random.Next(540, 1200));

        StartCoroutine(PlanMeteor(6, false, result =>
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
        currentTimeOffset = random.Next(minTimeBetweenSpawns, maxTimeBetweenSpawns);

        var amountToSpawn = random.Next(1, maxToSpawn);
        
        for (var i = 0; i < amountToSpawn; i++)
        {
            Plugin.Logger.LogInfo(amountToSpawn.ToString());
            Plugin.Logger.LogInfo("starting plan meteor");
            StartCoroutine(PlanMeteor());
        }
    }

    private IEnumerator PlanMeteor(int maxAttempts = 4, bool spawn = true, Action<bool> callback = null)
    {
        Plugin.Logger.LogInfo("Starting PlanMeteor coroutine.");
        bool result = false;
        for (var i = 0; i < maxAttempts; i++)
        {
            Plugin.Logger.LogInfo($"Attempt {i+1}/{maxAttempts}");
            // var initialPos = RoundManager.Instance.outsideAINodes[random.Next(0, RoundManager.Instance.outsideAINodes.Length)].transform.position;
            var landLocation = RoundManager.Instance.outsideAINodes[random.Next(0, RoundManager.Instance.outsideAINodes.Length)].transform.position;
            var spawnLocation = landLocation + meteorSpawnLocationOffset;
            var raycastHit = Physics.RaycastAll(spawnLocation, landLocation, Mathf.Infinity, ~layersToIgnore);
            Plugin.Logger.LogDebug($"Casted ray. {raycastHit}, {raycastHit.Length}");
            if (raycastHit.Any(hit => hit.transform && hit.transform.tag == "Wood"))
            {
                Plugin.Logger.LogInfo("Raycast blocked by an object not tagged 'Wood'");
                yield return null;
                continue;
            }

            if (!spawn) 
            {
                result = true;
                break;
            }

            var timeAtSpawn = NetworkManager.Singleton.LocalTime.Time + (random.NextDouble() * 10 + 2);
            var meteorInstance = Instantiate(meteorPrefab, spawnLocation, Quaternion.identity, Plugin.effectObject.transform);
            meteorInstance.GetComponent<NetworkObject>().Spawn(false);
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
