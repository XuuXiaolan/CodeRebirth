using System;
using System.Collections.Generic;
using System.Linq;
using CodeRebirth.src.Content.Unlockables;
using CodeRebirth.src.Util;
using GameNetcodeStuff;

namespace CodeRebirth.src.Content.Items;
public class Zorti : GrabbableObject
{

    private bool beenUsed = false;
    public override void ItemActivate(bool used, bool buttonDown = true)
    {
        base.ItemActivate(used, buttonDown);
        if (beenUsed) return;

        playerHeldBy.StartCoroutine(playerHeldBy.waitToEndOfFrameToDiscard());
        grabbable = false;
        beenUsed = true;
        // do animation where it hovers, particle effects etc, then despawn it
    }

    public override void OnNetworkDespawn()
    {
        base.OnNetworkDespawn();
        
        System.Random rand = new System.Random(StartOfRound.Instance.randomMapSeed + 69);
        int randomNumber = rand.Next(0, 100);
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
                deadPlayers.Add(player);
            }
        }

        if (deadPlayers.Count > 0)
        {
            SCP999GalAI.DoStuffToRevivePlayer(this.transform.position, Array.IndexOf(StartOfRound.Instance.allPlayerScripts, deadPlayers[rand.Next(0, deadPlayers.Count)]));
        }
        else if (randomNumber <= 70)
        {
            if (IsServer) RoundManager.Instance.SpawnEnemyGameObject(this.transform.position, -1, -1, CodeRebirthUtils.EnemyTypes.Where(x => x.enemyName == "Masked").FirstOrDefault());
        }
    }
}