using System;
using CodeRebirth.MapStuff;
using CodeRebirth.EnemyStuff;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.InputSystem;
using Random = System.Random;
using Unity.Mathematics;

namespace CodeRebirth.Util.Spawning;
internal class CodeRebirthUtils : NetworkBehaviour
{
    static Random random;
    internal static CodeRebirthUtils Instance { get; private set; }
    
    void Awake()
    {
        Instance = this;
    }
    
    [ServerRpc(RequireOwnership = false)]
    public void SpawnScrapServerRpc(string itemName, Vector3 position, bool isQuest = false) {
        if (StartOfRound.Instance == null) {
            return;
        }
        
        if (random == null) {
            random = new Random(StartOfRound.Instance.randomMapSeed + 85);
        }

        if (itemName == string.Empty) {
            return;
        }
        Plugin.samplePrefabs.TryGetValue(itemName, out Item item);
        if (item == null)
        {
            return;
        }
        GameObject go = Instantiate(item.spawnPrefab, position + Vector3.up * 0.2f, Quaternion.identity);
        go.GetComponent<NetworkObject>().Spawn();
        int value = random.Next(minValue: item.minValue, maxValue: item.maxValue);
        var scanNode = go.GetComponentInChildren<ScanNodeProperties>();
        scanNode.scrapValue = value;
        scanNode.subText = $"Value: ${value}";
        go.GetComponent<GrabbableObject>().scrapValue = value;
        UpdateScanNodeClientRpc(new NetworkObjectReference(go), value);
        if (isQuest) go.AddComponent<QuestItem>();
    }

    [ClientRpc]
    public void UpdateScanNodeClientRpc(NetworkObjectReference go, int value) {
        go.TryGet(out NetworkObject netObj);
        if(netObj != null)
        {
            var scanNode = netObj.GetComponentInChildren<ScanNodeProperties>();
            scanNode.scrapValue = value;
            scanNode.subText = $"Value: ${value}";
        }
    }
}