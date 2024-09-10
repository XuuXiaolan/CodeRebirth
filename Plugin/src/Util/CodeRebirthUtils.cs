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
    public static Dictionary<string, GameObject> Objects = new Dictionary<string, GameObject>();
    
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
    public void SpawnDevilPropsServerRpc() {
        Objects.Add("Devil", Spawn("Devil", new Vector3(-19.355f, -1.473f, -0.243f), Quaternion.Euler(-81.746f, 152.088f, -53.711f)));
        Objects.Add("DevilChair", Spawn("DevilChair", new Vector3(-21.16f, -2.686f, 0), Quaternion.Euler(0, 180, 0)));
        Objects.Add("DevilTable", Spawn("DevilTable", new Vector3(-19.518f, -2.686f, 0), Quaternion.identity));
        Objects.Add("PlayerChair", Spawn("PlayerChair", new Vector3(-17.832f, -2.686f, 0), Quaternion.Euler(-90, -90, 0)));
    }

    [ServerRpc(RequireOwnership = false)]
    public void DespawnDevilPropsServerRpc() {
        foreach (KeyValuePair<string, GameObject> @object in Objects)
        {
            @object.Value.GetComponent<NetworkObject>().Despawn(true);
        }
        Objects.Clear();
    }
    
    public static GameObject Spawn(string objectName, Vector3 location, Quaternion rotation)
    {
        GameObject obj = Instantiate<GameObject>(MapObjectHandler.DevilDealPrefabs[objectName], location, rotation);
        NetworkObject component = obj.GetComponent<NetworkObject>();
        Plugin.ExtendedLogging(obj.name + " NetworkObject spawned");
        component.Spawn(false);
        return obj;
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

        if (StartOfRound.Instance.allPlayerScripts[playerID] == GameNetworkManager.Instance.localPlayerController) {
            // apply to all players
            foreach (PlayerControllerB player in StartOfRound.Instance.allPlayerScripts) {
                if(!player.isPlayerControlled) continue;
                Dealer.ApplyEffects(player);
            }
            
            for (int i = 0; i < Math.Abs(CodeRebirthSave.Current.MoonPriceUpgrade); i++) {
                if (CodeRebirthSave.Current.MoonPriceUpgrade > 0) Dealer.DecreaseMoonPrices();
                else Dealer.IncreaseMoonPrices();
            }
        } else
            Dealer.ApplyEffects(StartOfRound.Instance.allPlayerScripts[playerID]); // only apply to new player
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