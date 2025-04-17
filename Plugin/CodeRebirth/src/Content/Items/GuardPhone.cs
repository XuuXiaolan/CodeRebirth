using CodeRebirth.src.Content.Enemies;
using CodeRebirth.src.Content.Maps;
using UnityEngine;

namespace CodeRebirth.src.Content.Items;
public class GuardPhone : GrabbableObject
{

    public override void ItemActivate(bool used, bool buttonDown = true)
    {
        base.ItemActivate(used, buttonDown);
        if (StartOfRound.Instance.inShipPhase || !StartOfRound.Instance.shipHasLanded || StartOfRound.Instance.shipIsLeaving)
            return;

        if (!IsServer)
            return;

        if (playerHeldBy == null)
            return;

        Vector3 position = RoundManager.Instance.GetRandomNavMeshPositionInRadius(playerHeldBy.transform.position + playerHeldBy.transform.forward * 5f, 5f, default);

        RoundManager.Instance.SpawnEnemyGameObject(position, -1, -1, MapObjectHandler.Instance.Merchant.EnemyDefinitions.GetCREnemyDefinitionWithEnemyName("Guardsman").enemyType);
    }
}