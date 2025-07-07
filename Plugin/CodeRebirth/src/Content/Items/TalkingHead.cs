using System.Collections.Generic;
using System.Linq;
using CodeRebirth.src.ModCompats;
using CodeRebirth.src.Util;
using CodeRebirthLib.Util;
using CodeRebirthLib.Util.INetworkSerializables;
using GameNetcodeStuff;
using Unity.Netcode;
using UnityEngine;

namespace CodeRebirth.src.Content.Items;
public class TalkingHead : GrabbableObject
{
    public Transform playerBone = null!;
    public MeshRenderer meshRenderer = null!;
    [HideInInspector] public PlayerControllerB? player = null;
    private bool wasInFactoryLastFrame = false;

    [HideInInspector] public static List<TalkingHead> talkingHeads = new();

    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();
        isInFactory = false;
        talkingHeads.Add(this);
    }

    public override void OnNetworkDespawn()
    {
        base.OnNetworkDespawn();
        talkingHeads.Remove(this);
    }

    public override void Update()
    {
        base.Update();
        if (player == null)
            return;

        if (isInFactory != wasInFactoryLastFrame && GameNetworkManager.Instance.localPlayerController == player && RoundManager.Instance.currentLevel.planetHasTime)
        {
            Plugin.ExtendedLogging("Teleporting player.");
            var entranceTeleport = CodeRebirthLibNetworker.EntrancePoints.Where(p => p.isEntranceToBuilding == !wasInFactoryLastFrame).FirstOrDefault();
            entranceTeleport?.TeleportPlayer();
        }
        wasInFactoryLastFrame = this.isInFactory;
        if (GameNetworkManager.Instance.localPlayerController == player)
        {
            meshRenderer.enabled = false;
        }

        player.inSpecialInteractAnimation = true;
        player.playingQuickSpecialAnimation = true;
        player.thisPlayerModelLOD1.gameObject.SetActive(false);
        player.thisPlayerModelLOD2.gameObject.SetActive(false);
        player.thisPlayerModel.gameObject.SetActive(false);
        player.disableMoveInput = true;
        player.disableInteract = true;
        player.transform.SetPositionAndRotation(playerBone.position, transform.rotation);
        int alivePlayers = StartOfRound.Instance.allPlayerScripts.Where(player => player.isPlayerControlled && !player.isPlayerDead && !player.IsPseudoDead()).Count();
        if (StartOfRound.Instance.shipIsLeaving || StartOfRound.Instance.allPlayerScripts.Where(player => player.isPlayerControlled && !player.isPlayerDead && !player.IsPseudoDead()).Count() == 0)
        {
            MoreCompanySoftCompat.TryDisableOrEnableCosmetics(player, false);
            player.DisablePlayerModel(player.gameObject, true, false);
            player.disableMoveInput = false;
            player.disableInteract = false;
            player.thisPlayerModel.gameObject.SetActive(true);
            player.playerBadgeMesh.gameObject.SetActive(true);
            player.thisPlayerModelLOD2.gameObject.SetActive(true);
            player.thisPlayerModelLOD1.gameObject.SetActive(true);
            player.headCostumeContainer.gameObject.SetActive(true);
            player.headCostumeContainerLocal.gameObject.SetActive(true);
            player.playerBetaBadgeMesh.gameObject.SetActive(true);
            player.SetPseudoDead(false);
            player.gameObject.layer = 3;
            player.inSpecialInteractAnimation = false;
            player.playingQuickSpecialAnimation = false;
            if (GameNetworkManager.Instance.localPlayerController == player)
            {
                HUDManager.Instance.HideHUD(false);
                StartOfRound.Instance.allowLocalPlayerDeath = true;
                player.KillPlayer(Vector3.zero, false, CauseOfDeath.Snipped, 0);
            }
            Plugin.ExtendedLogging($"Killing talking head.");
            player = null;
        }
    }

    [ServerRpc(RequireOwnership = false)]
    public void SyncTalkingHeadServerRpc(PlayerControllerReference playerControllerReference)
    {
        SyncTalkingHeadClientRpc(playerControllerReference);
    }

    [ClientRpc]
    private void SyncTalkingHeadClientRpc(PlayerControllerReference playerControllerReference)
    {
        player = playerControllerReference;
        if (GameNetworkManager.Instance.localPlayerController == player)
        {
            meshRenderer.enabled = false;
        }
        player.SetPseudoDead(true);
    }
}