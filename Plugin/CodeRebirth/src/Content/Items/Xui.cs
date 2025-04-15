using System.Collections;
using System.Collections.Generic;
using System.Linq;
using CodeRebirth.src.Content.Unlockables;
using CodeRebirth.src.Util;
using GameNetcodeStuff;
using UnityEngine;

namespace CodeRebirth.src.Content.Items;
public class Xui : GrabbableObject
{

    private bool beenUsed = false;
    private System.Random rand = new();
    public override void Start()
    {
        base.Start();
        rand = new System.Random(StartOfRound.Instance.randomMapSeed + 69);
    }

    public override void ItemActivate(bool used, bool buttonDown = true)
    {
        base.ItemActivate(used, buttonDown);
        if (beenUsed || StartOfRound.Instance.inShipPhase || StartOfRound.Instance.shipIsLeaving || !StartOfRound.Instance.shipHasLanded) return;

        playerHeldBy.StartCoroutine(playerHeldBy.waitToEndOfFrameToDiscard());
        grabbable = false;
        grabbableToEnemies = false;
        beenUsed = true;
        StartCoroutine(DoAnimation(1f));
        // do animation where it hovers, particle effects etc, then despawn it
    }

    public override void FallWithCurve()
    {
        if (beenUsed)
        {
            this.transform.position += new Vector3(0, 0.2f * Time.deltaTime, 0);
            return;
        }
        base.FallWithCurve();
    }

    public IEnumerator DoAnimation(float delay)
    {
        if (!IsServer) yield break;
        yield return new WaitForSeconds(delay);
        NetworkObject.Despawn();
    }

    public override void OnNetworkDespawn()
    {
        base.OnNetworkDespawn();
        int randomNumber = rand.Next(100);
        Plugin.ExtendedLogging($"Random Number: {randomNumber}");
        if (randomNumber <= 35)
        {
            if (IsServer) RoundManager.Instance.SpawnEnemyGameObject(this.transform.position, -1, -1, CodeRebirthUtils.EnemyTypes.Where(x => x.enemyName == "Masked").FirstOrDefault());
            return;
        }
        List<PlayerControllerB> deadPlayers = new();
        foreach (var player in StartOfRound.Instance.allPlayerScripts)
        {
            if (player.isPlayerDead && player.spectatedPlayerScript != null)
            {
                Plugin.ExtendedLogging($"Player {player.name} is dead.");
                deadPlayers.Add(player);
            }
        }

        if (deadPlayers.Count > 0)
        {
            SCP999GalAI.DoStuffToRevivePlayer(this.transform.position, System.Array.IndexOf(StartOfRound.Instance.allPlayerScripts, deadPlayers[rand.Next(deadPlayers.Count)]));
        }
        else if (randomNumber <= 70)
        {
            if (IsServer) RoundManager.Instance.SpawnEnemyGameObject(this.transform.position, -1, -1, CodeRebirthUtils.EnemyTypes.Where(x => x.enemyName == "Masked").FirstOrDefault());
        }
    }
}