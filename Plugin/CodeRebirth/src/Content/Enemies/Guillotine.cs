using System.Linq;
using CodeRebirth.src.Content.Items;
using CodeRebirth.src.ModCompats;
using CodeRebirth.src.Util;
using CodeRebirthLib;
using CodeRebirthLib.Utils;

using GameNetcodeStuff;
using Unity.Netcode;
using UnityEngine;

namespace CodeRebirth.src.Content.Enemies;
public class Guillotine : NetworkBehaviour
{
    public Transform scrapSpawnTransform = null!;
    public Transform playerBone = null!;
    public AudioSource audioSource = null!;
    public AudioClip GuillotineSound = null!;
    internal Mistress mistress = null!;

    [HideInInspector] public PlayerControllerB? playerToKill = null;
    [HideInInspector] public bool sequenceFinished = false;

    public void Update()
    {
        if (!sequenceFinished && playerToKill != null)
        {
            playerToKill.transform.SetPositionAndRotation(playerBone.position, playerBone.rotation);
        }
    }

    public void FinishGuillotineSequenceAnimEvent()
    {
        sequenceFinished = true;
        if (playerToKill != null && playerToKill.IsLocalPlayer())
        {
            PseudoKillPlayerServerRpc(playerToKill);
        }
    }

    [ServerRpc(RequireOwnership = false)]
    public void SyncGuillotineServerRpc(PlayerControllerReference playerControllerReference, NetworkBehaviourReference mistressNetworkBehaviourReference)
    {
        SyncGuillotineClientRpc(playerControllerReference, mistressNetworkBehaviourReference);
    }

    [ClientRpc]
    public void SyncGuillotineClientRpc(PlayerControllerReference playerControllerReference, NetworkBehaviourReference mistressNetworkBehaviourReference)
    {
        playerToKill = playerControllerReference;
        mistress = (Mistress)mistressNetworkBehaviourReference;
        if (playerToKill.IsLocalPlayer() && playerToKill.isCrouching)
        {
            playerToKill.Crouch(false);
        }
    }

    [ServerRpc(RequireOwnership = false)]
    public void PseudoKillPlayerServerRpc(PlayerControllerReference playerControllerReference)
    {
        PseudoKillPlayerClientRpc(playerControllerReference);
    }

    [ClientRpc]
    private void PseudoKillPlayerClientRpc(PlayerControllerReference playerControllerReference)
    {
        playerToKill = playerControllerReference;
        Plugin.ExtendedLogging($"Killing player {playerToKill}!");
        int alivePlayers = StartOfRound.Instance.allPlayerScripts.Where(player => player.isPlayerControlled && !player.isPlayerDead && !player.IsPseudoDead()).Count();
        if (StartOfRound.Instance.allPlayerScripts.Where(player => player.isPlayerControlled && !player.isPlayerDead && !player.IsPseudoDead()).Count() == 1)
        {
            if (playerToKill.IsLocalPlayer())
                playerToKill.KillPlayer(Vector3.zero, false, CauseOfDeath.Snipped, 0);

            return;
        }
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
        playerToKill.playerBetaBadgeMesh.gameObject.SetActive(false);
        playerToKill.inAnimationWithEnemy = mistress;
        if (GameNetworkManager.Instance.localPlayerController == playerToKill)
        {
            HUDManager.Instance.HideHUD(true);
            StartOfRound.Instance.allowLocalPlayerDeath = false;
        }
        if (IsServer)
        {
            GameObject talkingHead = (GameObject)CodeRebirthUtils.Instance.SpawnScrap(LethalContent.Items[CodeRebirthItemKeys.TalkingHead].Item, scrapSpawnTransform.position, false, true, 0);
            TalkingHead talkingHeadScript = talkingHead.GetComponent<TalkingHead>();
            talkingHeadScript.player = playerToKill;
            talkingHeadScript.mistress = mistress;
            talkingHeadScript.SyncTalkingHeadServerRpc(playerToKill, new NetworkBehaviourReference(mistress));
        }
        playerToKill = null;
    }

    public void PlayGuillotineSoundAnimEvent()
    {
        audioSource.PlayOneShot(GuillotineSound);
    }
}