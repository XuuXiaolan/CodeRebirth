using System;
using System.Collections;
using System.Collections.Generic;
using CodeRebirth.src.Content.Items;
using CodeRebirth.src.Content.Maps;
using CodeRebirth.src.Util;
using CodeRebirth.src.Util.Extensions;
using GameNetcodeStuff;
using Unity.Netcode;
using UnityEngine;

namespace CodeRebirth.src.Content.Unlockables;

public class PlantPot : NetworkBehaviour // Add saving of stages to this thing
{
    public InteractTrigger trigger = null!;
    public Transform[] ItemSpawnSpots = null!;
    public GameObject[] enableList = null!;
    public enum Stage
    {
        Zero = 0,
        One = 1,
        Two = 2,
        Three = 3,
    }

    public enum FruitType
    {
        None = 0,
        Tomato = 1,
        Golden_Tomato = 2,
    }

    
    // point to new data structure because i do not feel like changing it
    public NetworkVariable<int> fruitType = new NetworkVariable<int>(0, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);
    public NetworkVariable<int> stage = new NetworkVariable<int>(0, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);
    
    
    [NonSerialized] public bool grewThisOrbit = true;

    [NonSerialized] public static List<PlantPot> Instances = new();

    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();
        Instances.Add(this);
        if (!IsServer) return;
        LoadPlantData();
    }

    public void Start()
    {
        if ((Stage)stage.Value != Stage.Zero)
        {
            StartCoroutine(GrowthRoutine());
            enableList[stage.Value].SetActive(true);
            return;
        }
        if ((Stage)stage.Value == Stage.Zero) 
        {
            trigger.onInteract.AddListener(OnInteract);
        }
    }

    public void SavePlantData()
    {
        ES3.Save<int>(this.gameObject.name + "Stage", stage.Value, GameNetworkManager.Instance.currentSaveFileName);
        ES3.Save<int>(this.gameObject.name + "FruitType", fruitType.Value, GameNetworkManager.Instance.currentSaveFileName);
    }

    public void LoadPlantData()
    {
        stage.Value = ES3.Load<int>(this.gameObject.name + "Stage", GameNetworkManager.Instance.currentSaveFileName, 0);
        fruitType.Value = ES3.Load<int>(this.gameObject.name + "FruitType", GameNetworkManager.Instance.currentSaveFileName, 0);
        Plugin.ExtendedLogging($"Loaded stage {stage} and fruit type {fruitType}");
    }

    [ServerRpc(RequireOwnership = false)]
    private void StartPlantGrowthServerRpc(NetworkObjectReference netObjRef)
    {
        StartCoroutine(GrowthRoutine());
        IncreaseStageClientRpc();
        SetupFruitTypeClientRpc(netObjRef);
    }

    [ClientRpc]
    private void SetupFruitTypeClientRpc(NetworkObjectReference netObjRef)
    {
        WoodenSeed woodenSeed = ((GameObject)netObjRef).GetComponent<WoodenSeed>();
        fruitType.Value = (int)woodenSeed.fruitType;
        Plugin.ExtendedLogging($"Setting up fruit type {fruitType}");
        woodenSeed.playerHeldBy.DespawnHeldObject();
    }

    private void OnInteract(PlayerControllerB playerInteracting)
    {
        if (playerInteracting == null || playerInteracting != GameNetworkManager.Instance.localPlayerController) return;
        if (playerInteracting.currentlyHeldObjectServer != null && playerInteracting.currentlyHeldObjectServer.itemProperties.itemName == "Wooden Seed")
        {
            StartPlantGrowthServerRpc(new NetworkObjectReference(playerInteracting.currentlyHeldObjectServer.NetworkObject));
        }
    }

    private IEnumerator GrowthRoutine() 
    {
        trigger.enabled = false;
        while (true)
        {
            yield return new WaitUntil(() => StartOfRound.Instance.inShipPhase && !grewThisOrbit && RoundManager.Instance.currentLevel.spawnEnemiesAndScrap);
            if ((Stage)stage.Value < Stage.Three)
            {
                IncreaseStageServerRpc();
            }
            else
            {
                switch ((FruitType)fruitType.Value) 
                {
                    case FruitType.Tomato:
                        ProduceFruitServerRpc((int)FruitType.Tomato);
                        break;
                    case FruitType.Golden_Tomato:
                        ProduceFruitServerRpc((int)FruitType.Golden_Tomato);
                        break;
                }
            }
            grewThisOrbit = true;
            yield return new WaitForEndOfFrame();
        }
    }

    [ServerRpc(RequireOwnership = false)]
    private void ProduceFruitServerRpc(int fruitType)
    {
        Item? itemToSpawn = null;
        switch (fruitType)
        {
            case (int)FruitType.Tomato:
                itemToSpawn = UnlockableHandler.Instance.PlantPot.Tomato;
                break;
            case (int)FruitType.Golden_Tomato:
                itemToSpawn = UnlockableHandler.Instance.PlantPot.GoldenTomato;
                break;
        }
        if (itemToSpawn == null)
        {
            Plugin.Logger.LogError($"Couldn't find fruit of type {fruitType}!");
            return;
        }
        foreach (var itemSpawnSpot in ItemSpawnSpots)
        {
            CodeRebirthUtils.Instance.SpawnScrap(itemToSpawn, itemSpawnSpot.position, false, true, 0);
        }
    }

    [ServerRpc(RequireOwnership = false)]
    private void IncreaseStageServerRpc()
    {
        IncreaseStageClientRpc();
    }

    [ClientRpc]
    private void IncreaseStageClientRpc()
    {
        Plugin.ExtendedLogging($"Increasing stage from {stage.Value} to {stage.Value + 1}");
        if ((Stage)stage.Value == Stage.Zero) trigger.onInteract.RemoveListener(OnInteract);
        enableList[stage.Value].SetActive(false);
        stage.Value++;
        enableList[stage.Value].SetActive(true);
    }

    public override void OnNetworkDespawn() 
    {
        base.OnNetworkDespawn();
        Instances.Remove(this);
        StopAllCoroutines();
    }
}
