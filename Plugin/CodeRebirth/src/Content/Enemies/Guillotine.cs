using System;
using CodeRebirth.src.Content.Items;
using CodeRebirth.src.Util;
using GameNetcodeStuff;
using Unity.Netcode;
using UnityEngine;

namespace CodeRebirth.src.Content.Enemies;
public class Guillotine : MonoBehaviour
{
    public Transform scrapSpawnTransform = null!;
    public Transform playerBone = null!;
    [HideInInspector] public PlayerControllerB playerToKill = null!;
    [HideInInspector] public bool sequenceFinished = false;

    public void LateUpdate()
    {
        if (!sequenceFinished)
        {
            playerToKill.transform.position = playerBone.position;
            playerToKill.transform.rotation = playerBone.rotation;
        }
    }

    public void FinishGuillotineSequenceAnimEvent()
    {
        sequenceFinished = true;
        if (playerToKill == null || playerToKill.isPlayerDead) return;
        playerToKill.isPlayerDead = true;
        playerToKill.disableMoveInput = true;
        playerToKill.disableInteract = true;
        playerToKill.DisablePlayerModel(playerToKill.gameObject, false, true);
        if (!NetworkManager.Singleton.IsServer) return;
        GameObject talkingHead = (GameObject)CodeRebirthUtils.Instance.SpawnScrap(EnemyHandler.Instance.Mistress.ChoppedTalkingHead, scrapSpawnTransform.position, false, true, 0);
        TalkingHead talkingHeadScript = talkingHead.GetComponent<TalkingHead>();
        talkingHeadScript.player = playerToKill;
        talkingHeadScript.SyncTalkingHeadServerRpc(Array.IndexOf(StartOfRound.Instance.allPlayerScripts, playerToKill));
    }
}