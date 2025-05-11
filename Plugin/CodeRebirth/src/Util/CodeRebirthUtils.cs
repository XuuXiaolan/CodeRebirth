using CodeRebirth.src.Content.Enemies;
using Unity.Netcode;
using UnityEngine;
using System.Collections.Generic;
using CodeRebirth.src.MiscScripts;
using UnityEngine.Rendering;
using CodeRebirth.src.Content.Unlockables;
using System.Linq;
using UnityEngine.Rendering.HighDefinition;
using CodeRebirth.src.Content.Items;
using CodeRebirth.src.Util.Extensions;
using GameNetcodeStuff;

namespace CodeRebirth.src.Util;
internal class CodeRebirthUtils : NetworkBehaviour
{
    public Material WireframeMaterial = null!;
    public Shader SeeThroughShader = null!;
    public Volume TimeSlowVolume = null!;
    public Volume FireyVolume = null!;
    public Volume SmokyVolume = null!;
    public Volume CloseEyeVolume = null!;
    public Volume StaticCloseEyeVolume = null!;
    public AnimationClip ModifiedShipLandAnimation = null!;
    public AnimationClip ModifiedDangerousShipLeaveAnimation = null!;
    public AnimationClip ModifiedShipLeaveAnimation = null!;

    [HideInInspector] public static List<EnemyType> EnemyTypes = new();
    [HideInInspector] public static EntranceTeleport[] entrancePoints = [];
    [HideInInspector] public int collidersAndRoomAndDefaultAndInteractableAndRailingAndEnemiesAndTerrainAndHazardAndVehicleMask = 0;
    [HideInInspector] public int collidersAndRoomAndInteractableAndRailingAndEnemiesAndTerrainAndHazardAndVehicleMask = 0;
    [HideInInspector] public int collidersAndRoomAndInteractableAndRailingAndTerrainAndHazardAndVehicleMask = 0;
    [HideInInspector] public int collidersAndRoomAndPlayersAndEnemiesAndTerrainAndVehicleAndDefaultMask = 0;
    [HideInInspector] public int collidersAndRoomAndRailingAndTerrainAndHazardAndVehicleAndDefaultMask = 0;
    [HideInInspector] public int collidersAndRoomAndPlayersAndEnemiesAndTerrainAndVehicleMask = 0;
    [HideInInspector] public int collidersAndRoomAndRailingAndInteractableMask = 0;
    [HideInInspector] public int collidersAndRoomAndPlayersAndInteractableMask = 0;
    [HideInInspector] public int playersAndInteractableAndEnemiesAndPropsHazardMask = 0;
    [HideInInspector] public int collidersAndRoomMaskAndDefaultAndEnemies = 0;
    [HideInInspector] public int playersAndEnemiesAndHazardMask = 0;
    [HideInInspector] public int playersAndRagdollMask = 0;
    [HideInInspector] public int propsAndHazardMask = 0;
    [HideInInspector] public int terrainAndFoliageMask = 0;
    [HideInInspector] public int playersAndEnemiesMask = 0;
    [HideInInspector] public int defaultMask = 0;
    [HideInInspector] public int propsMask = 0;
    [HideInInspector] public int hazardMask = 0;
    [HideInInspector] public int enemiesMask = 0;
    [HideInInspector] public int interactableMask = 0;
    [HideInInspector] public int railingMask = 0;
    [HideInInspector] public int terrainMask = 0;
    [HideInInspector] public int vehicleMask = 0;
    [HideInInspector] public ES3Settings SaveSettings;
    [HideInInspector] public ShipAnimator shipAnimator = null!;
    [HideInInspector] public StartMatchLever startMatchLever = null!;
    [HideInInspector] public Terminal shipTerminal = null!;
    [HideInInspector] public static HashSet<(Light light, HDAdditionalLightData hDAdditionalLightData)> currentRoundLightData = new();
    [HideInInspector] public System.Random CRRandom = new();
    internal static CodeRebirthUtils Instance { get; private set; } = null!;

    private void Awake()
    {
        CRRandom = new System.Random(StartOfRound.Instance.randomMapSeed);
        StartOfRound.Instance.StartNewRoundEvent.AddListener(OnNewRoundStart);
        DoLayerMaskStuff();
        SaveSettings = new($"CR{GameNetworkManager.Instance.currentSaveFileName}", ES3.EncryptionType.None);
        Instance = this;
        shipTerminal = FindFirstObjectByType<Terminal>(FindObjectsInactive.Exclude);
        startMatchLever = FindFirstObjectByType<StartMatchLever>(FindObjectsInactive.Exclude);
        shipAnimator = StartOfRound.Instance.shipAnimatorObject.gameObject.AddComponent<ShipAnimator>();
        shipAnimator.shipLandAnimation = ModifiedShipLandAnimation;
        shipAnimator.shipNormalLeaveAnimation = ModifiedShipLeaveAnimation;
        StartCoroutine(ProgressiveUnlockables.LoadUnlockedIDs());
    }

    private void DoLayerMaskStuff()
    {
        defaultMask = LayerMask.GetMask("Default");
        propsMask = LayerMask.GetMask("Props");
        hazardMask = LayerMask.GetMask("MapHazards");
        enemiesMask = LayerMask.GetMask("Enemies");
        interactableMask = LayerMask.GetMask("InteractableObject");
        railingMask = LayerMask.GetMask("Railing");
        terrainMask = LayerMask.GetMask("Terrain");
        vehicleMask = LayerMask.GetMask("Vehicle");
        playersAndRagdollMask = StartOfRound.Instance.playersMask | LayerMask.GetMask("PlayerRagdoll");
        propsAndHazardMask = propsMask | hazardMask;
        terrainAndFoliageMask = terrainMask | LayerMask.GetMask("Foliage");
        playersAndEnemiesMask = StartOfRound.Instance.playersMask | enemiesMask;
        playersAndEnemiesAndHazardMask = playersAndEnemiesMask | hazardMask;
        collidersAndRoomMaskAndDefaultAndEnemies = StartOfRound.Instance.collidersAndRoomMaskAndDefault | enemiesMask;
        collidersAndRoomAndRailingAndInteractableMask = StartOfRound.Instance.collidersAndRoomMask | interactableMask | railingMask;
        collidersAndRoomAndPlayersAndInteractableMask = StartOfRound.Instance.collidersAndRoomMaskAndPlayers | interactableMask;
        playersAndInteractableAndEnemiesAndPropsHazardMask = playersAndEnemiesAndHazardMask | interactableMask | propsMask;
        collidersAndRoomAndRailingAndTerrainAndHazardAndVehicleAndDefaultMask = StartOfRound.Instance.collidersAndRoomMask | hazardMask | railingMask | terrainMask | vehicleMask | defaultMask;
        collidersAndRoomAndPlayersAndEnemiesAndTerrainAndVehicleMask = StartOfRound.Instance.collidersAndRoomMaskAndPlayers | enemiesMask | terrainMask | vehicleMask;
        collidersAndRoomAndPlayersAndEnemiesAndTerrainAndVehicleAndDefaultMask = collidersAndRoomAndPlayersAndEnemiesAndTerrainAndVehicleMask | defaultMask;
        collidersAndRoomAndInteractableAndRailingAndTerrainAndHazardAndVehicleMask = collidersAndRoomAndRailingAndInteractableMask | hazardMask | terrainMask | vehicleMask;
        collidersAndRoomAndInteractableAndRailingAndEnemiesAndTerrainAndHazardAndVehicleMask = collidersAndRoomAndInteractableAndRailingAndTerrainAndHazardAndVehicleMask | enemiesMask;
        collidersAndRoomAndDefaultAndInteractableAndRailingAndEnemiesAndTerrainAndHazardAndVehicleMask = collidersAndRoomAndInteractableAndRailingAndEnemiesAndTerrainAndHazardAndVehicleMask | defaultMask;
    }

    public void PlayerPressedJump(PlayerControllerB player)
    {
        foreach (var instance in Mountaineer.Instances)
        {
            instance.JumpActionTriggered(player);
        }
    }

    public void PlayerPressedInteract(PlayerControllerB player)
    {
        foreach (var instance in Mountaineer.Instances)
        {
            instance.InteractActionTriggered(player);
        }
    }

    public void OnNewRoundStart()
    {
        entrancePoints = FindObjectsByType<EntranceTeleport>(FindObjectsSortMode.InstanceID);
        foreach (var entrance in entrancePoints)
        {
            if (!entrance.FindExitPoint())
            {
                Plugin.Logger.LogError("Something went wrong in the generation of the fire exits");
            }
        }
    }

    public void UnlockProgressively(int unlockableIndex, int playerIndex, bool local, bool displayTip, string messageHeader, string messagBody)
    {
        UnlockableItem unlockable = ProgressiveUnlockables.unlockableIDs.Keys.ElementAt(unlockableIndex);
        ProgressiveUnlockables.unlockableIDs[unlockable] = true;
        unlockable.unlockableName = ProgressiveUnlockables.unlockableNames[unlockableIndex];
        if (!displayTip) return;
        if (local && GameNetworkManager.Instance.localPlayerController != StartOfRound.Instance.allPlayerScripts[playerIndex]) return;
        HUDManager.Instance.DisplayTip(messageHeader, messagBody);
    }

    [ServerRpc(RequireOwnership = false)]
    public void RequestProgressiveUnlocksFromHostServerRpc(int playerIndex, bool displayTip, bool local, string messageHeader, string messagBody)
    {
        for (int i = 0; i < ProgressiveUnlockables.unlockableIDs.Count; i++)
        {
            if (!ProgressiveUnlockables.unlockableIDs.Values.ElementAt(i)) continue;
            RequestProgressiveUnlocksFromHostClientRpc(i, playerIndex, local, displayTip, messageHeader, messagBody);
        }
    }

    [ClientRpc]
    public void RequestProgressiveUnlocksFromHostClientRpc(int unlockableID, int playerIndex, bool local, bool displayTip, string messageHeader, string messagBody)
    {
        UnlockProgressively(unlockableID, playerIndex, local, displayTip, messageHeader, messagBody);
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

    private int _indexToSpawn = 0;
    [ServerRpc(RequireOwnership = false)]
    public void SpawnRandomCRHazardServerRpc(Vector3 position)
    {
        Plugin.ExtendedLogging("Spawning CR Hazard with index: " + _indexToSpawn);
        if (position == Vector3.zero)
        {
            Plugin.Logger.LogError("Trying to spawn a hazard at Vector3.zero!");
            return;
        }
        GameObject hazardToSpawn = CodeRebirthRegistry.RegisteredCRMapObjects[_indexToSpawn].gameObject;
        if (hazardToSpawn != null)
        {
            var go = GameObject.Instantiate(hazardToSpawn, position, Quaternion.identity, RoundManager.Instance.mapPropsContainer.transform);
            go.GetComponent<NetworkObject>().Spawn(true);
        }
        _indexToSpawn++;
        if (_indexToSpawn >= CodeRebirthRegistry.RegisteredCRMapObjects.Count - 1)
            _indexToSpawn = 0;
    }

    [ServerRpc(RequireOwnership = false)]
    public void SpawnScrapServerRpc(string? itemName, Vector3 position, bool isQuest = false, bool defaultRotation = true, int valueIncrease = 0)
    {
        if (itemName == string.Empty || itemName == null)
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

    public NetworkObjectReference SpawnScrap(Item? item, Vector3 position, bool isQuest, bool defaultRotation, int valueIncrease)
    {
        if (StartOfRound.Instance == null || item == null)
        {
            return default;
        }

        Transform? parent = null;
        if (parent == null)
        {
            parent = StartOfRound.Instance.propsContainer;
        }
        Vector3 spawnPosition = position + Vector3.up * 0.2f;
        GameObject go = Instantiate(item.spawnPrefab, spawnPosition, defaultRotation ? Quaternion.Euler(item.restingRotation) : Quaternion.identity, parent);
        NetworkObject networkObject = go.GetComponent<NetworkObject>();
        GrabbableObject grabbableObject = go.GetComponent<GrabbableObject>();
        grabbableObject.fallTime = 0;
        networkObject.Spawn();
        UpdateParentServerRpc(new NetworkObjectReference(go));

        int value = (int)(UnityEngine.Random.Range(item.minValue, item.maxValue) * RoundManager.Instance.scrapValueMultiplier) + valueIncrease;
        ScanNodeProperties? scanNodeProperties = go.GetComponentInChildren<ScanNodeProperties>();
        if (scanNodeProperties != null)
        {
            scanNodeProperties.scrapValue = value;
            scanNodeProperties.subText = $"Value: ${value}";
            grabbableObject.scrapValue = value;
            UpdateScanNodeServerRpc(new NetworkObjectReference(networkObject), value);
        }

        if (isQuest)
        {
            StartUIForItemServerRpc(go);
        }
        return new NetworkObjectReference(go);
    }

    [ServerRpc(RequireOwnership = false)]
    public void UpdateParentServerRpc(NetworkObjectReference go)
    {
        UpdateParentClientRpc(go);
    }

    [ClientRpc]
    public void UpdateParentClientRpc(NetworkObjectReference go)
    {
        go.TryGet(out NetworkObject netObj);
        if (netObj != null)
        {
            netObj.transform.parent = StartOfRound.Instance.propsContainer;
        }
    }

    [ServerRpc(RequireOwnership = false)]
    public void UpdateScanNodeServerRpc(NetworkObjectReference go, int value)
    {
        UpdateScanNodeClientRpc(go, value);
    }

    [ClientRpc]
    public void UpdateScanNodeClientRpc(NetworkObjectReference go, int value)
    {
        go.TryGet(out NetworkObject netObj);
        if (netObj != null)
        {
            if (netObj.gameObject.TryGetComponent(out GrabbableObject grabbableObject))
            {
                grabbableObject.SetScrapValue(value);
                Plugin.ExtendedLogging($"Scrap Value: {value}");
            }
        }
    }

    [ServerRpc(RequireOwnership = false)]
    public void StartUIForItemServerRpc(NetworkObjectReference go)
    {
        StartUIForItemClientRpc(go);
    }

    [ClientRpc]
    public void StartUIForItemClientRpc(NetworkObjectReference go)
    {
        DuckUI.Instance.itemUI.StartUIforItem(((GameObject)go).GetComponent<PhysicsProp>());
        ((GameObject)go).AddComponent<QuestItem>();
    }

    public void SaveCodeRebirthData()
    {
        if (!NetworkManager.Singleton.IsHost) return;
        PiggyBank.Instance?.SaveCurrentCoins();
        foreach (var plantpot in PlantPot.Instances)
        {
            plantpot.SavePlantData();
        }
        ProgressiveUnlockables.SaveUnlockedIDs();
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

    public T? CreateFallingObject<T>(GameObject prefab, Vector3 origin, Vector3 target, float speed) where T : FallingObjectBehaviour
    {
        Plugin.ExtendedLogging($"creating falling object: {prefab.name} going from {origin} to {target}");
        GameObject gameObject = Instantiate(prefab, origin, Quaternion.identity);
        T fallingObjectBehaviour = gameObject.GetComponent<T>();

        if (fallingObjectBehaviour == null)
        {
            Plugin.Logger.LogError($"FallingObjectBehaviour Component on GameObject: {prefab} does not exist.");
            return null;
        }
        fallingObjectBehaviour.NetworkObject.OnSpawn(() =>
        {
            fallingObjectBehaviour.SetupFallingObjectServerRpc(origin, target, speed);
        });
        fallingObjectBehaviour.NetworkObject.Spawn();

        return fallingObjectBehaviour;
    }
}