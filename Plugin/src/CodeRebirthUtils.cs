using Unity.Netcode;
using UnityEngine;
using UnityEngine.Profiling;
using static System.Net.Mime.MediaTypeNames;

namespace CodeRebirth.src;
internal class CodeRebirthUtils : NetworkBehaviour
{
    static int seed = 0;
    static System.Random random;
    internal static CodeRebirthUtils Instance { get; set; }

    void Awake()
    {
        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    [ServerRpc(RequireOwnership = false)]
    public void SpawnScrapServerRpc(string itemName, Vector3 position) {
        Plugin.Logger.LogInfo("yippee3!");
        if (StartOfRound.Instance == null)
        {
            Plugin.Logger.LogInfo("StartOfRound null");
            return;
        }
        if (random == null)
        {
            Plugin.Logger.LogInfo("Initializing random");
            seed = StartOfRound.Instance.randomMapSeed;
            random = new System.Random(seed + 85);
        }

        if (itemName.Length == 0)
        {
            Plugin.Logger.LogInfo("itemName is empty");
            return;
        }
        Plugin.samplePrefabs.TryGetValue(itemName, out Item item);
        if (item == null)
        {
            Plugin.Logger.LogInfo($"Could not get Item {itemName}");
            return;
        }
        GameObject go = Instantiate(item.spawnPrefab, position + Vector3.up, Quaternion.identity);
        int value = random.Next(minValue: item.minValue, maxValue: item.maxValue);
        var scanNode = go.GetComponentInChildren<ScanNodeProperties>();
        scanNode.scrapValue = value;
        scanNode.subText = $"Value: ${value}";
        go.GetComponent<GrabbableObject>().scrapValue = value;
        go.GetComponent<NetworkObject>().Spawn(false);
        UpdateScanNodeClientRpc(new NetworkObjectReference(go), value);
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