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
using CodeRebirth.src.Content.Maps;
using System.Collections;
using CodeRebirth.src.Content.Weathers;
using LethalLevelLoader;
using CodeRebirthLib.ContentManagement.Items;
using CodeRebirthLib.ContentManagement.MapObjects;
using CodeRebirthLib.ContentManagement.Weathers;
using CodeRebirthLib.Util.INetworkSerializables;

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

    internal static List<EnemyType> EnemyTypes = new();
    internal ES3Settings SaveSettings;
    internal ShipAnimator shipAnimator = null!;
    internal StartMatchLever startMatchLever = null!;
    internal Terminal shipTerminal = null!;
    internal static HashSet<(Light light, HDAdditionalLightData hDAdditionalLightData)> currentRoundLightData = new();
    internal Dictionary<EnemyType, float> enemyCoinDropRate = new();
    internal System.Random CRRandom = new();
    internal static CodeRebirthUtils Instance { get; private set; } = null!;

    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();
        Instance = this;
        StartCoroutine(HandleEnemyDropRates());
        CRRandom = new System.Random(StartOfRound.Instance.randomMapSeed + 69);
        SaveSettings = new($"CR{GameNetworkManager.Instance.currentSaveFileName}", ES3.EncryptionType.None);
        shipTerminal = FindFirstObjectByType<Terminal>(FindObjectsInactive.Exclude);
        startMatchLever = FindFirstObjectByType<StartMatchLever>(FindObjectsInactive.Exclude);
        shipAnimator = StartOfRound.Instance.shipAnimatorObject.gameObject.AddComponent<ShipAnimator>();
        shipAnimator.shipLandAnimation = ModifiedShipLandAnimation;
        shipAnimator.shipNormalLeaveAnimation = ModifiedShipLeaveAnimation;

        LevelManager.TryGetExtendedLevel(StartOfRound.Instance.levels.Where(x => x.sceneName == "Oxyde").FirstOrDefault(), out ExtendedLevel? extendedLevel);
        Plugin.ExtendedLogging($"Extended level: {extendedLevel?.SelectableLevel}");
        if (extendedLevel == null)
            return;

        CheckWithHostToUnlockOxydeServerRpc(new NetworkExtendedLevelReference(extendedLevel));
        StartCoroutine(HandleWRSetupWithOxyde(extendedLevel));
    }

    private IEnumerator HandleWRSetupWithOxyde(ExtendedLevel oxydeExtendedLevel)
    {
        yield return new WaitUntil(() => WeatherRegistry.WeatherManager.IsSetupFinished);

        if (WeatherHandler.Instance.NightShift == null)
            yield break;

        if (TimeOfDay.Instance.daysUntilDeadline <= 0)
        {
            WeatherRegistry.WeatherController.ChangeWeather(oxydeExtendedLevel.SelectableLevel, LevelWeatherType.None);
            yield break;
        }

        if (!Plugin.Mod.WeatherRegistry().TryGetFromWeatherName("night shift", out CRWeatherDefinition? nightShiftWeatherDefinition))
            yield break;

        Plugin.ExtendedLogging($"Night shift weather: {nightShiftWeatherDefinition.Weather}");
        WeatherRegistry.WeatherController.ChangeWeather(oxydeExtendedLevel.SelectableLevel, nightShiftWeatherDefinition.Weather);
    }

    [ServerRpc(RequireOwnership = false)]
    private void CheckWithHostToUnlockOxydeServerRpc(NetworkExtendedLevelReference extendedLevelNetworkReference)
    {
        if (extendedLevelNetworkReference.TryGet(out ExtendedLevel? extendedLevel) && extendedLevel != null)
        {
            Plugin.ExtendedLogging($"Extended level is real: {extendedLevel.SelectableLevel}");
            if (Plugin.ModConfig.ConfigOxydeEnabledFromStart.Value)
            {
                extendedLevel.IsRouteHidden = false;
                extendedLevel.IsRouteLocked = false;
            }
            CheckWithHostToUnlockOxydeClientRpc(extendedLevelNetworkReference, extendedLevel.IsRouteLocked);
        }
    }

    [ClientRpc]
    private void CheckWithHostToUnlockOxydeClientRpc(NetworkExtendedLevelReference extendedLevelNetworkReference, bool lockOxyde)
    {
        if (extendedLevelNetworkReference.TryGet(out ExtendedLevel? extendedLevel) && extendedLevel != null)
        {
            extendedLevel.IsRouteHidden = lockOxyde;
            extendedLevel.IsRouteLocked = lockOxyde;
        }
    }

    private IEnumerator HandleEnemyDropRates()
    {
        yield return new WaitUntil(() => EnemyTypes.Count > 0);
        if (MapObjectHandler.Instance.Merchant == null)
            yield break;

        if (!Plugin.Mod.MapObjectRegistry().TryGetFromMapObjectName("Money", out CRMapObjectDefinition? moneyMapObjectDefinition))
            yield break;

        var enemyWithRarityDropRate = moneyMapObjectDefinition.GetGeneralConfig<string>("Money | Enemy Drop Rates").Value.Split(',').Select(s => s.Trim());
        foreach (var enemyWithRarity in enemyWithRarityDropRate)
        {
            var split = enemyWithRarity.Split(':');
            EnemyType? enemyType = EnemyTypes.Where(et => et.enemyName.Equals(split[0], System.StringComparison.OrdinalIgnoreCase)).FirstOrDefault();
            if (enemyType == null)
            {
                Plugin.Logger.LogWarning($"Couldn't find enemy of name '{split[0]}' for the money drop rate config!");
                continue;
            }
            float dropRate = float.Parse(split[1], System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture);
            Plugin.ExtendedLogging($"{enemyType.enemyName} has a drop rate of {dropRate}");
            enemyCoinDropRate.Add(enemyType, dropRate);
        }
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

    [ServerRpc(RequireOwnership = false)]
    public void DamagePlayerOnOwnerServerRpc(PlayerControllerReference playerControllerB, int damageAmount, bool hasDamageSFX, int causeOfDeathInt, int deathAnimationInt, bool fallDamage, Vector3 force)
    {
        DamagePlayerOnOwnerClientRpc(playerControllerB, damageAmount, hasDamageSFX, causeOfDeathInt, deathAnimationInt, fallDamage, force);
    }

    [ClientRpc]
    public void DamagePlayerOnOwnerClientRpc(PlayerControllerReference playerControllerB, int damageAmount, bool hasDamageSFX, int causeOfDeathInt, int deathAnimationInt, bool fallDamage, Vector3 force)
    {
        PlayerControllerB playerBeingDamaged = playerControllerB;
        DamagePlayerOnOwner(playerBeingDamaged, damageAmount, hasDamageSFX, causeOfDeathInt, deathAnimationInt, fallDamage, force);
    }

    private void DamagePlayerOnOwner(PlayerControllerB playerBeingDamaged, int damageNumber, bool hasDamageSFX, int causeOfDeathInt, int deathAnimationInt, bool fallDamage, Vector3 force)
    {
        playerBeingDamaged.DamagePlayer(damageNumber, hasDamageSFX, true, (CauseOfDeath)causeOfDeathInt, deathAnimationInt, fallDamage, force);
    }

    [ServerRpc(RequireOwnership = false)]
    public void KillPlayerOnOwnerServerRpc(PlayerControllerReference playerControllerB, bool spawnBody, int causeOfDeathInt, int deathAnimationInt, Vector3 positionOffset)
    {
        KillPlayerOnOwnerClientRpc(playerControllerB, spawnBody, causeOfDeathInt, deathAnimationInt, positionOffset);
    }

    [ClientRpc]
    public void KillPlayerOnOwnerClientRpc(PlayerControllerReference playerControllerB, bool spawnBody, int causeOfDeathInt, int deathAnimationInt, Vector3 positionOffset)
    {
        PlayerControllerB playerBeingDamaged = playerControllerB;
        KillPlayerOnOwner(playerBeingDamaged, spawnBody, causeOfDeathInt, deathAnimationInt, positionOffset);
    }

    private void KillPlayerOnOwner(PlayerControllerB playerBeingDamaged, bool spawnBody, int causeOfDeathInt, int deathAnimationInt, Vector3 positionOffset)
    {
        playerBeingDamaged.KillPlayer(playerBeingDamaged.velocityLastFrame, spawnBody, (CauseOfDeath)causeOfDeathInt, deathAnimationInt, positionOffset);
    }


    [ServerRpc(RequireOwnership = false)]
    public void ReactToVehicleCollisionServerRpc(int obstacleId)
    {
        ReactToVehicleCollisionClientRpc(obstacleId);
    }

    [ClientRpc]
    public void ReactToVehicleCollisionClientRpc(int obstacleId)
    {
        if (ReactToVehicleCollision.TryGetById(obstacleId, out ReactToVehicleCollision? reactToVehicleCollision) && reactToVehicleCollision != null)
        {
            reactToVehicleCollision.InvokeCollisionEvent();
        }
        else
        {
            Plugin.Logger.LogError($"ReactToVehicleCollision with ID {obstacleId} not found!");
        }
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

        List<CRMapObjectDefinition> mapObjectDefinitions = Plugin.Mod.MapObjectRegistry().ToList();
        GameObject hazardToSpawn = mapObjectDefinitions[_indexToSpawn].GameObject;
        if (hazardToSpawn != null)
        {
            var go = GameObject.Instantiate(hazardToSpawn, position, Quaternion.identity, RoundManager.Instance.mapPropsContainer.transform);
            if (go.TryGetComponent(out NetworkObject networkObject))
            {
                networkObject.Spawn(true);
            }
            else
            {
                // if i wanted this sync'd i'd rpc the spawning
            }
        }

        _indexToSpawn++;
        if (_indexToSpawn >= mapObjectDefinitions.Count - 1)
            _indexToSpawn = 0;
    }

    [ServerRpc(RequireOwnership = false)]
    public void SpawnScrapServerRpc(string? itemName, Vector3 position, bool isQuest = false, bool defaultRotation = true, int valueIncrease = 0)
    {
        if (itemName == string.Empty || itemName == null)
        {
            return;
        }
        if (!Plugin.Mod.ItemRegistry().TryGetFromItemName(itemName, out CRItemDefinition? itemDefinition))
            return;

        if (itemDefinition.Item == null)
        {
            // throw for stacktrace
            Plugin.Logger.LogError($"'{itemName}' either isn't a CodeRebirth scrap or not registered! This method only handles CodeRebirth scrap!");
            return;
        }
        SpawnScrap(itemDefinition.Item, position, isQuest, defaultRotation, valueIncrease);
    }

    public NetworkObjectReference SpawnScrap(Item? item, Vector3 position, bool isQuest, bool defaultRotation, int valueIncrease, Quaternion rotation = default)
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

        Vector3 spawnPosition = position;
        if (Physics.Raycast(position + Vector3.up * 1f, Vector3.down, out RaycastHit hit, 100f, StartOfRound.Instance.collidersAndRoomMaskAndDefault, QueryTriggerInteraction.Ignore))
        {
            spawnPosition = hit.point + Vector3.up * item.verticalOffset;
        }

        GameObject go = Instantiate(item.spawnPrefab, spawnPosition + Vector3.up * 0.2f, Quaternion.identity, parent);
        GrabbableObject grabbableObject = go.GetComponent<GrabbableObject>();
        NetworkObject networkObject = grabbableObject.NetworkObject;
        grabbableObject.fallTime = 0;
        networkObject.Spawn();
        UpdateParentAndRotationsServerRpc(new NetworkObjectReference(go), defaultRotation ? Quaternion.Euler(item.restingRotation) : rotation);

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
    public void UpdateParentAndRotationsServerRpc(NetworkObjectReference go, Quaternion rotation)
    {
        UpdateParentAndRotationsClientRpc(go, rotation);
    }

    [ClientRpc]
    public void UpdateParentAndRotationsClientRpc(NetworkObjectReference go, Quaternion rotation)
    {
        go.TryGet(out NetworkObject netObj);
        if (netObj != null)
        {
            if (netObj.AutoObjectParentSync && IsServer)
            {
                netObj.transform.parent = StartOfRound.Instance.propsContainer; // only the server can reparent network objects error?
            }
            else if (!netObj.AutoObjectParentSync)
            {
                netObj.transform.parent = StartOfRound.Instance.propsContainer;
            }
            Plugin.ExtendedLogging($"This object just spawned: {netObj.gameObject.name}");
            StartCoroutine(ForceRotationForABit(netObj.gameObject, rotation));
        }
    }

    private IEnumerator ForceRotationForABit(GameObject go, Quaternion rotation)
    {
        float duration = 0.25f;
        while (duration > 0)
        {
            duration -= Time.deltaTime;
            go.transform.rotation = rotation;
            yield return null;
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
    }

    public static void ResetCodeRebirthData(ES3Settings saveSettings)
    {
        ES3.DeleteFile(saveSettings);
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