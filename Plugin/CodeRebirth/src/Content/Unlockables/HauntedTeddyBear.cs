using System;
using System.Collections.Generic;
using Dawn;
using Unity.Netcode;
using UnityEngine;

namespace CodeRebirth.src.Content.Unlockables;
public class HauntedTeddyBear : MonoBehaviour
{
    [field: SerializeField]
    public AutoParentToShip AutoParentToShip { get; private set; }

    [field: SerializeField]
    public List<Vector3> RandomSpawnPositions { get; private set; } = new();

    [field: SerializeField]
    public List<Vector3> RandomSpawnRotations { get; private set; } = new();

    internal static void Init()
    {
        On.StartOfRound.SetShipReadyToLand += TrySpawnHauntedTeddyBear;
    }

    private static void TrySpawnHauntedTeddyBear(On.StartOfRound.orig_SetShipReadyToLand orig, StartOfRound self)
    {
        orig(self);

        if (!NetworkManager.Singleton.IsServer)
        {
            return;
        }

        if (!LethalContent.Unlockables.TryGetValue(CodeRebirthUnlockableItemKeys.TeddyBear, out DawnUnlockableItemInfo teddyBearUnlockableItemInfo))
        {
            return;
        }

        UnlockableItem teddyUnlockable = teddyBearUnlockableItemInfo.UnlockableItem;
        int indexInList = StartOfRound.Instance.unlockablesList.unlockables.IndexOf(teddyUnlockable);
        if (indexInList == -1)
        {
            return;
        }

        StartOfRound.Instance.UnlockShipObject(indexInList);
    }

    public void Start()
    {
        ChangePosition();
    }

    public void ChangePosition()
    {
        if (RandomSpawnPositions.Count == 0)
        {
            return;
        }

        System.Random random = new(StartOfRound.Instance.randomMapSeed);
        int randomIndex = random.Next(0, RandomSpawnPositions.Count);
        Vector3 newPosition = RandomSpawnPositions[randomIndex];
        Vector3 newRotation = transform.rotation.eulerAngles;

        if (RandomSpawnRotations.Count > randomIndex)
        {
            newRotation = RandomSpawnRotations[randomIndex];
        }

        AutoParentToShip.positionOffset = newPosition;
        AutoParentToShip.rotationOffset = newRotation;
        AutoParentToShip.MoveToOffset();
    }
}