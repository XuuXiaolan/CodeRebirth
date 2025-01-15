using System;
using System.Collections;
using System.Collections.Generic;
using CodeRebirth.src.Content.Items;
using CodeRebirth.src.Util;
using GameNetcodeStuff;
using Unity.Netcode;
using UnityEngine;

namespace CodeRebirth.src.Content.Unlockables;

class PlantPotData {
    public PlantPot.FruitType fruitType = PlantPot.FruitType.None;
    public PlantPot.Stage stage = PlantPot.Stage.Zero;
}

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

    PlantPotData _data;
    
    // point to new data structure because i do not feel like changing it
    public FruitType fruitType
    {
        get => _data.fruitType;
        set => _data.fruitType = value;
    }

    public Stage stage
    {
        get => _data.stage;
        set => _data.stage = value;
    }
    
    
    [NonSerialized] public bool grewThisOrbit = true;
    private System.Random random = new System.Random();

    [NonSerialized] public static List<PlantPot> Instances = new();
    internal static int totalSpawnedPlantPots;
    int _thisPlantPotID;
    
    public IEnumerator Start()
    {
        yield return new WaitUntil(() => CodeRebirthUtils.Instance != null);
        
        _thisPlantPotID = totalSpawnedPlantPots;
        
        Plugin.ExtendedLogging($"plant pot id: {_thisPlantPotID}");

        
        // Because this is handled via reference it should just work
        if (!CodeRebirthSave.Current.PlantPotData.TryGetValue(_thisPlantPotID, out _data))
        {
            _data = new PlantPotData();
            CodeRebirthSave.Current.PlantPotData[_thisPlantPotID] = _data;
        }
        else
        {
            Plugin.ExtendedLogging("loaded from save!");
        }
        
        Plugin.ExtendedLogging($"stage: {_data.stage}");
        
        totalSpawnedPlantPots++;
        Instances.Add(this);

        random = new System.Random(StartOfRound.Instance.randomMapSeed);
        if (stage != Stage.Zero) enableList[(int)stage].SetActive(true);
        if (stage == Stage.Zero) 
        {
            trigger.onInteract.AddListener(OnInteract);
        }
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
        fruitType = woodenSeed.fruitType;
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
        while (true)
        {
            yield return new WaitUntil(() => StartOfRound.Instance.inShipPhase && !grewThisOrbit && RoundManager.Instance.currentLevel.levelID != 3);
            if (stage < Stage.Three)
            {
                IncreaseStageServerRpc();
            }
            else
            {
                switch (fruitType) 
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
        string itemToSpawn = "";
        switch (fruitType)
        {
            case (int)FruitType.Tomato:
                itemToSpawn = "Tomato";
                break;
            case (int)FruitType.Golden_Tomato:
                itemToSpawn = "Golden Tomato";
                break;
        }
        Plugin.samplePrefabs.TryGetValue(itemToSpawn, out Item item);
        foreach (var itemSpawnSpot in ItemSpawnSpots)
        {
            NetworkObjectReference spawnedItem = CodeRebirthUtils.Instance.SpawnScrap(item, itemSpawnSpot.position, false, true, 0);
            ((GameObject)spawnedItem).GetComponent<Fruit>().plantPot = this;
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
        Plugin.ExtendedLogging($"Increasing stage from {(int)stage} to {(int)stage + 1}");
        if (stage == Stage.Zero) trigger.onInteract.RemoveListener(OnInteract);
        enableList[(int)stage].SetActive(false);
        stage++;
        enableList[(int)stage].SetActive(true);
    }

    public override void OnNetworkDespawn() 
    {
        base.OnNetworkDespawn();
        Instances.Remove(this);
        StopAllCoroutines();
    }
}
