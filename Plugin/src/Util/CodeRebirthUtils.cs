using System;
using CodeRebirth.src.Content.Maps;
using CodeRebirth.src.Content.Enemies;
using Unity.Netcode;
using UnityEngine;
using Random = System.Random;
using System.Collections.Generic;
using CodeRebirth.src.Content;
using CodeRebirth.src.Util.Extensions;
using Mono.Cecil.Cil;
using Newtonsoft.Json;
using On.GameNetcodeStuff;
using PlayerControllerB = GameNetcodeStuff.PlayerControllerB;

namespace CodeRebirth.src.Util;
internal class CodeRebirthUtils : NetworkBehaviour
{
    private static Random random = null!;
    internal static CodeRebirthUtils Instance { get; private set; } = null!;
    
    void Awake()
    {
        Instance = this;
    }
    
    [ServerRpc(RequireOwnership = false)]
    public void SpawnScrapServerRpc(string itemName, Vector3 position, bool isQuest = false, bool defaultRotation = true, int valueIncrease = 0) {
        if (itemName == string.Empty) {
            return;
        }
        Plugin.samplePrefabs.TryGetValue(itemName, out Item item);
        if (item == null) {
            // throw for stacktrace
            throw new NullReferenceException($"'{itemName}' either isn't a CodeRebirth scrap or not registered! This method only handles CodeRebirth scrap!");
        }
        SpawnScrap(item, position, isQuest, defaultRotation, valueIncrease);
    }

    public NetworkObjectReference SpawnScrap(Item item, Vector3 position, bool isQuest, bool defaultRotation, int valueIncrease) {
        if (StartOfRound.Instance == null) {
            return default;
        }
        
        if (random == null) {
            random = new Random(StartOfRound.Instance.randomMapSeed + 85);
        }

        Transform? parent = null;
        if (parent == null) {
            parent = StartOfRound.Instance.propsContainer;
        }
        GameObject go = Instantiate(item.spawnPrefab, position + Vector3.up * 0.2f, defaultRotation == true ? Quaternion.Euler(item.restingRotation) : Quaternion.identity, parent);
        go.GetComponent<NetworkObject>().Spawn();
        int value = random.NextInt(item.minValue + valueIncrease, item.maxValue + valueIncrease);
        var scanNode = go.GetComponentInChildren<ScanNodeProperties>();
        scanNode.scrapValue = value;
        scanNode.subText = $"Value: ${value}";
        go.GetComponent<GrabbableObject>().scrapValue = value;
        UpdateScanNodeClientRpc(new NetworkObjectReference(go), value);
        if (isQuest) go.AddComponent<QuestItem>();
        return new NetworkObjectReference(go);
    }

    [ClientRpc]
    public void UpdateScanNodeClientRpc(NetworkObjectReference go, int value) {
        go.TryGet(out NetworkObject netObj);
        if(netObj != null)
        {
            if (netObj.gameObject.TryGetComponent(out GrabbableObject grabbableObject)) {
                grabbableObject.SetScrapValue(value);
                Plugin.ExtendedLogging($"Scrap Value: {value}");
            }
        }
    }

    [ServerRpc(RequireOwnership = false)]
    void RequestLoadSaveDataServerRPC(int playerID) {
        ulong steamID = StartOfRound.Instance.allPlayerScripts[playerID].playerSteamId;
        if (!CodeRebirthSave.Current.PlayerData.ContainsKey(steamID)) { CodeRebirthSave.Current.PlayerData[steamID] = new(); CodeRebirthSave.Current.Save();}
        SetSaveDataClientRPC(playerID, JsonConvert.SerializeObject(CodeRebirthSave.Current));
    }

    [ClientRpc]
    void SetSaveDataClientRPC(int playerID, string saveData) {
        Plugin.ExtendedLogging("Received save data from host!");

        if (!IsHost && !IsServer) {
            CodeRebirthSave.Current = JsonConvert.DeserializeObject<CodeRebirthSave>(saveData, new JsonSerializerSettings {
                ContractResolver = new IncludePrivateSetterContractResolver()
            })!;
        }
    }

    public override void OnNetworkSpawn() {
        if (IsHost || IsServer) {
            CodeRebirthSave.Current = PersistentDataHandler.Load<CodeRebirthSave>($"CRSave{GameNetworkManager.Instance.saveFileNum}");
        }
        RequestLoadSaveDataServerRPC(StartOfRound.Instance.ClientPlayerList[NetworkManager.Singleton.LocalClientId]);
    }

    void OnDisable() {
        CodeRebirthSave.Current = null!;
    }
}