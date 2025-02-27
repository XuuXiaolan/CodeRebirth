using CodeRebirth.src.Content.Enemies;
using Unity.Netcode;
using UnityEngine;
using System.Collections.Generic;
using CodeRebirth.src.Content.Maps;
using UnityEngine.AI;
using CodeRebirth.src.MiscScripts;
using UnityEngine.Rendering;
using CodeRebirth.src.Content.Unlockables;

namespace CodeRebirth.src.Util;
internal class CodeRebirthUtils : NetworkBehaviour
{
    public Material WireframeMaterial = null!;
    public Shader SeeThroughShader = null!;
    public Volume TimeSlowVolume = null!;
    public Volume FireyVolume = null!;
    public Volume SmokyVolume = null!;
    public Volume CloseEyeVolume = null!;

    [HideInInspector] public static List<EnemyType> EnemyTypes = new();
    [HideInInspector] public static EntranceTeleport[] entrancePoints = [];
    [HideInInspector] public int collidersAndRoomAndInteractableAndRailingAndEnemiesAndTerrainAndHazardAndVehicleMask = 0;
    [HideInInspector] public int collidersAndRoomAndRailingAndInteractableMask = 0;
    [HideInInspector] public int collidersAndRoomAndPlayersAndInteractableMask = 0;
    [HideInInspector] public int collidersAndRoomMaskAndDefaultAndEnemies = 0;
    [HideInInspector] public int playersAndEnemiesAndHazardMask = 0;
    [HideInInspector] public int playersAndRagdollMask = 0;
    [HideInInspector] public int propsAndHazardMask = 0;
    [HideInInspector] public int terrainAndFoliageMask = 0;
    [HideInInspector] public int propsMask = 0;
    [HideInInspector] public int hazardMask = 0;
    [HideInInspector] public int enemiesMask = 0;
    [HideInInspector] public int interactableMask = 0;
    [HideInInspector] public int collidersAndRoomAndPlayersAndEnemiesAndTerrainAndVehicleMask = 0;
    [HideInInspector] public ES3Settings SaveSettings;
    private System.Random CRRandom = null;
    internal static CodeRebirthUtils Instance { get; private set; } = null!;

    private void Awake()
    {
        StartOfRound.Instance.StartNewRoundEvent.AddListener(OnNewRoundStart);
        DoLayerMaskStuff();
        SaveSettings = new($"CR{GameNetworkManager.Instance.currentSaveFileName}", ES3.EncryptionType.None);
        Instance = this;
    }

    private void DoLayerMaskStuff()
    {
        collidersAndRoomAndInteractableAndRailingAndEnemiesAndTerrainAndHazardAndVehicleMask = StartOfRound.Instance.collidersAndRoomMask | LayerMask.GetMask("InteractableObject", "Railing", "Enemies", "Terrain", "MapHazards", "Vehicle");
        collidersAndRoomAndRailingAndInteractableMask = StartOfRound.Instance.collidersAndRoomMask | LayerMask.GetMask("Railing", "InteractableObject");
        collidersAndRoomAndPlayersAndInteractableMask = StartOfRound.Instance.collidersAndRoomMaskAndPlayers | LayerMask.GetMask("InteractableObject");
        collidersAndRoomMaskAndDefaultAndEnemies = StartOfRound.Instance.collidersAndRoomMaskAndDefault | LayerMask.GetMask("Enemies");
        playersAndEnemiesAndHazardMask = LayerMask.GetMask("Player", "Enemies", "MapHazards");
        playersAndRagdollMask = LayerMask.GetMask("Player", "PlayerRagdoll");
        propsAndHazardMask = LayerMask.GetMask("Props", "MapHazards");
        terrainAndFoliageMask = LayerMask.GetMask("Terrain", "Foliage");
        propsMask = LayerMask.GetMask("Props");
        hazardMask = LayerMask.GetMask("MapHazards");
        enemiesMask = LayerMask.GetMask("Enemies");
        interactableMask = LayerMask.GetMask("InteractableObject");
        collidersAndRoomAndPlayersAndEnemiesAndTerrainAndVehicleMask = StartOfRound.Instance.collidersAndRoomMaskAndPlayers | LayerMask.GetMask("Enemies", "Terrain", "Vehicle");
    }

    public void OnNewRoundStart()
    {
        entrancePoints = FindObjectsByType<EntranceTeleport>(FindObjectsSortMode.InstanceID);
    }

    [ServerRpc(RequireOwnership = false)]
    public void SpawnHazardServerRpc(Vector3 position)
    {
        // Get a random prefab to spawn from the hazard prefabs list
        GameObject prefabToSpawn = MapObjectHandler.hazardPrefabs[0];

        // Remove the prefab from the list to prevent re-spawning it directly
        MapObjectHandler.hazardPrefabs.RemoveAt(0);

        // Get a random position on the NavMesh
        NavMeshHit hit = default;
        Vector3 positionToSpawn = RoundManager.Instance.GetRandomNavMeshPositionInRadiusSpherical(position, 2f, hit);

        // Instantiate a new instance of the prefab
        GameObject spawnedObject = GameObject.Instantiate(prefabToSpawn, positionToSpawn, Quaternion.identity, RoundManager.Instance.mapPropsContainer.transform);
        
        // Align the object's up direction with the hit normal
        spawnedObject.transform.up = hit.normal;

        NetworkObject networkObject = spawnedObject.GetComponent<NetworkObject>();
        networkObject?.Spawn(true);
        
        // Re-add the prefab back to the list if needed
        MapObjectHandler.hazardPrefabs.Add(prefabToSpawn);
    }

    [ServerRpc(RequireOwnership = false)]
    public void SpawnEnemyServerRpc(Vector3 position, string enemyName)
    {
        if (position == Vector3.zero)
        {
            Plugin.Logger.LogError("Trying to spawn an enemy at Vector3.zero!");
            return;
        }

        foreach (EnemyType enemyType in EnemyTypes)
        {
            Plugin.ExtendedLogging("Trying to spawn: " + enemyType.enemyName);
            if (enemyType.enemyName == enemyName)
            {
                RoundManager.Instance.SpawnEnemyGameObject(position, -1, 0, enemyType);
                return;
            }
        }
        Plugin.Logger.LogError($"Couldn't find enemy of name '{enemyName}'!");
    }

    [ServerRpc(RequireOwnership = false)]
    public void SpawnScrapServerRpc(string itemName, Vector3 position, bool isQuest = false, bool defaultRotation = true, int valueIncrease = 0)
    {
        if (itemName == string.Empty)
        {
            return;
        }
        Plugin.samplePrefabs.TryGetValue(itemName, out Item item);
        if (item == null)
        {
            // throw for stacktrace
            Plugin.Logger.LogError($"'{itemName}' either isn't a CodeRebirth scrap or not registered! This method only handles CodeRebirth scrap!");
            return;
        }
        SpawnScrap(item, position, isQuest, defaultRotation, valueIncrease);
    }

    public NetworkObjectReference SpawnScrap(Item item, Vector3 position, bool isQuest, bool defaultRotation, int valueIncrease)
    {
        if (StartOfRound.Instance == null)
        {
            return default;
        }
        
        CRRandom ??= new System.Random(StartOfRound.Instance.randomMapSeed + 85);

        Transform? parent = null;
        if (parent == null)
        {
            parent = StartOfRound.Instance.propsContainer;
        }
        GameObject go = Instantiate(item.spawnPrefab, position + Vector3.up * 0.2f, defaultRotation == true ? Quaternion.Euler(item.restingRotation) : Quaternion.identity, parent);
        go.GetComponent<NetworkObject>().Spawn();
        int value = (int)(CRRandom.Next(item.minValue, item.maxValue) * RoundManager.Instance.scrapValueMultiplier) + valueIncrease;
        var scanNode = go.GetComponentInChildren<ScanNodeProperties>();
        scanNode.scrapValue = value;
        scanNode.subText = $"Value: ${value}";
        go.GetComponent<GrabbableObject>().scrapValue = value;
        UpdateScanNodeClientRpc(new NetworkObjectReference(go), value);
        if (isQuest)
        {
            StartUIForItemClientRpc(go);
            go.AddComponent<QuestItem>();
        }
        return new NetworkObjectReference(go);
    }

    [ClientRpc]
    public void StartUIForItemClientRpc(NetworkObjectReference go)
    {
        DuckUI.Instance.itemUI.StartUIforItem(((GameObject)go).GetComponent<PhysicsProp>());
    }

    [ClientRpc]
    public void UpdateScanNodeClientRpc(NetworkObjectReference go, int value)
    {
        go.TryGet(out NetworkObject netObj);
        if(netObj != null)
        {
            if (netObj.gameObject.TryGetComponent(out GrabbableObject grabbableObject))
            {
                grabbableObject.SetScrapValue(value);
                Plugin.ExtendedLogging($"Scrap Value: {value}");
            }
        }
    }

    public void SaveCodeRebirthData()
    {
        if (!NetworkManager.Singleton.IsHost) return;
        PiggyBank.Instance?.SaveCurrentCoins();
		foreach (var plantpot in PlantPot.Instances)
		{
			plantpot.SavePlantData();
		}
    }

    public static void ResetCodeRebirthData(ES3Settings saveSettings)
    {
        ES3.DeleteFile(saveSettings);
    }

    [ServerRpc(RequireOwnership = false)]
    public void ResetEntrancePointsServerRpc()
    {
        ResetEntrancePointsClientRpc();
    }

    [ClientRpc]
    public void ResetEntrancePointsClientRpc()
    {
        entrancePoints = [];
    }
}