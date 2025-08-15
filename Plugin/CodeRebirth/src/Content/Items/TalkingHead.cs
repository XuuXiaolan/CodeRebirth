using System.Collections.Generic;
using System.Linq;
using CodeRebirth.src.Content.Enemies;
using CodeRebirth.src.ModCompats;
using CodeRebirth.src.Util;
using CodeRebirthLib.Internal;
using CodeRebirthLib.Utils;
using GameNetcodeStuff;
using Unity.Netcode;
using UnityEngine;

namespace CodeRebirth.src.Content.Items;
public class TalkingHead : GrabbableObject
{
    public Transform playerBone = null!;
    public Vector3 rotationOffset = new Vector3(90, 0, 0);
    public Vector3 positionOffset = new Vector3(-2.5f, -2.2f, 0);
    public MeshRenderer meshRenderer = null!;
    internal Mistress? mistress = null;
    internal PlayerControllerB? player = null;
    private bool wasInFactoryLastFrame = false;

    internal static List<TalkingHead> talkingHeads = new();

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

        if (mistress != null)
        {
            player.inAnimationWithEnemy = mistress;
        }
        player.thisPlayerModelLOD1.gameObject.SetActive(false);
        player.thisPlayerModelLOD2.gameObject.SetActive(false);
        player.thisPlayerModel.gameObject.SetActive(false);
        player.disableMoveInput = true;
        player.disableInteract = true;
        if (!isHeld)
        {
            Quaternion rotation = transform.rotation;
            rotation *= Quaternion.Euler(rotationOffset);
            player.transform.SetPositionAndRotation(playerBone.position + positionOffset, rotation);
        }
        int alivePlayers = StartOfRound.Instance.allPlayerScripts.Where(player => player.isPlayerControlled && !player.isPlayerDead && !player.IsPseudoDead()).Count();
        if (StartOfRound.Instance.shipIsLeaving || StartOfRound.Instance.allPlayerScripts.Where(player => player.isPlayerControlled && !player.isPlayerDead && !player.IsPseudoDead()).Count() == 0)
        {
            MoreCompanySoftCompat.TryDisableOrEnableCosmetics(player, false);
            player.DisablePlayerModel(player.gameObject, true, false);
            player.inAnimationWithEnemy = null;
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
    public void SyncTalkingHeadServerRpc(PlayerControllerReference playerControllerReference, NetworkBehaviourReference networkBehaviourReference)
    {
        SyncTalkingHeadClientRpc(playerControllerReference, networkBehaviourReference);
    }

    [ClientRpc]
    private void SyncTalkingHeadClientRpc(PlayerControllerReference playerControllerReference, NetworkBehaviourReference networkBehaviourReference)
    {
        player = playerControllerReference;
        mistress = (Mistress)networkBehaviourReference;
        if (GameNetworkManager.Instance.localPlayerController == player)
        {
            meshRenderer.enabled = false;
        }
        player.SetPseudoDead(true);
    }
}