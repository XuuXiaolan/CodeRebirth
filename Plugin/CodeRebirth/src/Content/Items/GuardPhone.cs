using System.Collections;
using CodeRebirth.src.Content.Enemies;
using CodeRebirth.src.Content.Maps;
using CodeRebirthLib.ContentManagement.Enemies;
using CodeRebirthLib.ContentManagement.Items;
using UnityEngine;

namespace CodeRebirth.src.Content.Items;
public class GuardPhone : GrabbableObject
{
    [SerializeField]
    private AudioSource _callSource = null!;

    [SerializeField]
    private AudioClip _callSound = null!;

    public override void ItemActivate(bool used, bool buttonDown = true)
    {
        base.ItemActivate(used, buttonDown);
        if (StartOfRound.Instance.inShipPhase || !StartOfRound.Instance.shipHasLanded || StartOfRound.Instance.shipIsLeaving || playerHeldBy == null || playerHeldBy.isInsideFactory)
        {
            currentUseCooldown = 0f;
            return;
        }

        if (!IsServer)
            return;

        Vector3 position = RoundManager.Instance.GetRandomNavMeshPositionInRadius(playerHeldBy.transform.position + playerHeldBy.transform.forward * 5f, 5f, default);
        StartCoroutine(SpawnWithDelay(position));

    }

    private IEnumerator SpawnWithDelay(Vector3 position)
    {
        _callSource.PlayOneShot(_callSound);
        yield return new WaitForSeconds(_callSound.length);
        yield return new WaitForSeconds(3f);
        if (!Plugin.Mod.EnemyRegistry().TryGetFromEnemyName("Guardsman", out CREnemyDefinition? guardsmanEnemyDefinition))
            yield break;

        RoundManager.Instance.SpawnEnemyGameObject(position, -1, -1, guardsmanEnemyDefinition.EnemyType);
    }
}