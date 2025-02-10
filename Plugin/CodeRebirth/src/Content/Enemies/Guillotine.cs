using System;
using System.Linq;
using CodeRebirth.src.Content.Items;
using CodeRebirth.src.ModCompats;
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

    public void Update()
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
        Plugin.ExtendedLogging($"Killing player {playerToKill}!");
        /*if (StartOfRound.Instance.allPlayerScripts.Where(player => player.isPlayerControlled && !player.isPlayerDead && !player.IsPsuedoDead()).Count() == 1)
        {
            playerToKill.KillPlayer(Vector3.zero, false, CauseOfDeath.Snipped, 0);
            return;
        }*/
        // if this is the last person left alive, then just kill em.
        MoreCompanySoftCompat.TryDisableOrEnableCosmetics(playerToKill, true);
        playerToKill.DropAllHeldItems();
        playerToKill.DisablePlayerModel(playerToKill.gameObject, false, true);
        playerToKill.disableMoveInput = true;
        playerToKill.disableInteract = true;
        playerToKill.playerBadgeMesh.gameObject.SetActive(false);
        playerToKill.thisPlayerModelLOD2.gameObject.SetActive(false);
        playerToKill.thisPlayerModelLOD1.gameObject.SetActive(false);
        playerToKill.headCostumeContainer.gameObject.SetActive(false);
        playerToKill.headCostumeContainerLocal.gameObject.SetActive(false);
        if (GameNetworkManager.Instance.localPlayerController == playerToKill)
        {
            HUDManager.Instance.HideHUD(true);
            StartOfRound.Instance.allowLocalPlayerDeath = false;
        }
        if (!NetworkManager.Singleton.IsServer) return;
        GameObject talkingHead = (GameObject)CodeRebirthUtils.Instance.SpawnScrap(EnemyHandler.Instance.Mistress.ChoppedTalkingHead, scrapSpawnTransform.position, false, true, 0);
        TalkingHead talkingHeadScript = talkingHead.GetComponent<TalkingHead>();
        talkingHeadScript.player = playerToKill;
        talkingHeadScript.SyncTalkingHeadServerRpc(Array.IndexOf(StartOfRound.Instance.allPlayerScripts, playerToKill));
    }
}