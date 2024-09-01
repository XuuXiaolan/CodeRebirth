using System.Collections;
using CodeRebirth.src.Util;
using CodeRebirth.src.Util.Extensions;
using GameNetcodeStuff;
using Unity.Netcode;
using UnityEngine;

namespace CodeRebirth.src.Content.Unlockables;
public class PlantPot : NetworkBehaviour // Add saving of stages to this thing
{
    public InteractTrigger trigger = null!;
    public Transform ItemSpawnSpot = null!;
    public GameObject[] enableList = null!;
    public enum Stage {
        Zero = 0,
        One = 1,
        Two = 2,
        Three = 3,
    }

    public enum FruitType {
        None = -1,
        Tomato = 0,
        Golden_Tomato = 1,
    }

    public FruitType fruitType = FruitType.None;
    public Stage stage = Stage.Zero;
    private System.Random random = new System.Random();
    public void Start()
    {
        random = new System.Random(StartOfRound.Instance.randomMapSeed);
        for (int i = 1; i <= enableList.Length; i++) {
            enableList[i].SetActive(i <= (int)stage);
        }
        if (stage == Stage.Zero) 
        {
            trigger.onInteract.AddListener(OnInteract);
        }
        else
        {
            StartCoroutine(GrowthRoutine());
        }
    }

    private void OnInteract(PlayerControllerB playerInteracting)
    {
        if (playerInteracting != GameNetworkManager.Instance.localPlayerController) return;
        if (playerInteracting.currentlyHeldObjectServer.itemProperties.itemName == "Wooden Seed") {
            playerInteracting.DespawnHeldObjectServerRpc();
            trigger.onInteract.RemoveListener(OnInteract);
            stage = Stage.One;
            StartCoroutine(GrowthRoutine());
        }
    }

    private IEnumerator GrowthRoutine() 
    
    {
        while (true) {
            yield return new WaitForSeconds(random.NextFloat(100f, 200f) * (stage == Stage.Three ? 0.5f : 1f));
            if (stage < Stage.Three) {
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
        }
    }

    [ServerRpc(RequireOwnership = false)]
    private void ProduceFruitServerRpc(int fruitType)
    {
        string itemToSpawn = "";
        switch (fruitType) {
            case (int)FruitType.Tomato:
                itemToSpawn = "Tomato";
                break;
            case (int)FruitType.Golden_Tomato:
                itemToSpawn = "Golden Tomato";
                break;
        }
        Plugin.samplePrefabs.TryGetValue(itemToSpawn, out Item item);
        CodeRebirthUtils.Instance.SpawnScrap(item, ItemSpawnSpot.position, false, true, 0);
    }

    [ServerRpc(RequireOwnership = false)]
    private void IncreaseStageServerRpc()
    {
        IncreaseStageClientRpc();
    }

    [ClientRpc]
    private void IncreaseStageClientRpc()
    {
        stage++;
    }

    public override void OnNetworkDespawn() 
    {
        base.OnNetworkDespawn();
        StopAllCoroutines();
        // save stage here?
        // save fruit type too maybe
    }
}
