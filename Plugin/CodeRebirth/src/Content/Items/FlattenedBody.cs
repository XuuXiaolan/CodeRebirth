using System;
using System.Collections;
using GameNetcodeStuff;
using Unity.Netcode;
using UnityEngine;

namespace CodeRebirth.src.Content.Items;
public class FlattenedBody : GrabbableObject
{
    internal PlayerControllerB? _flattenedBodyName = null;

    [SerializeField]
    private ScanNodeProperties _scanNodeProperties = null!;

    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();
        if (!IsServer)
            return;

        StartCoroutine(WaitUntilNameSet());

        /*foreach (var level in StartOfRound.Instance.levels)
        {
            float cumulativeWeight = 0;
            float zeddogWeight = 0;
            foreach (var item in level.spawnableScrap)
            {
                cumulativeWeight += item.rarity;
                if (item.spawnableItem.itemName.Equals("Zed Dog", StringComparison.OrdinalIgnoreCase))
                {
                    zeddogWeight = item.rarity;
                }
            }
            float chances = (zeddogWeight / cumulativeWeight) * 100;
            Plugin.Logger.LogFatal($"Level {level.sceneName} has a cumulative weight of {cumulativeWeight} and a zed dog weight of {zeddogWeight} so the chances are {chances} for each scrap rolling");
            Plugin.Logger.LogError($"that same level rolls inbetween min: {level.minScrap} and max: {level.maxScrap} so the actual chance is {(level.minScrap + level.maxScrap)/2 * chances}");
        }*/
    }

    private IEnumerator WaitUntilNameSet()
    {
        yield return null;
        yield return null;
        if (_flattenedBodyName == null)
            yield break;

        ChangeScanNodeNameServerRpc($"{_scanNodeProperties.headerText} Of {_flattenedBodyName.playerUsername}");
    }

    [ServerRpc]
    private void ChangeScanNodeNameServerRpc(string newName)
    {
        ChangeScanNodeNameClientRpc(newName);
    }

    [ClientRpc]
    private void ChangeScanNodeNameClientRpc(string newName)
    {
        _scanNodeProperties.headerText = newName;
    }
}