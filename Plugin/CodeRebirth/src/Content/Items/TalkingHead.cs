using System.Collections.Generic;
using System.Linq;
using CodeRebirth.src.Content.Enemies;
using CodeRebirth.src.ModCompats;
using CodeRebirth.src.Util;
using Dawn.Internal;
using Dawn.Utils;
using GameNetcodeStuff;
using Unity.Netcode;
using UnityEngine;

namespace CodeRebirth.src.Content.Items;
public class TalkingHead : GrabbableObject
{
    public Transform playerBone = null!;
    public Vector3 positionOffset = new Vector3(-2.5f, -2.2f, 0);
    public MeshRenderer meshRenderer = null!;
    internal Mistress? mistress = null;
    internal PlayerControllerB? player = null;
    private bool wasInFactoryLastFrame = false;
    private ScanNodeProperties scanNodeProperties = null!;
    private Vector3 rotationOffset = new Vector3(-90, 90, 90);
    private Vector3 nonHeldRotationOffset = new Vector3(90, 0, 0);
    private Renderer localHeadRenderer = null;

    internal static List<TalkingHead> talkingHeads = new();

    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();
        scanNodeProperties = GetComponentInChildren<ScanNodeProperties>();
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
            var entranceTeleport = DawnNetworker.EntrancePoints.FirstOrDefault(p => p.isEntranceToBuilding == !wasInFactoryLastFrame);
            entranceTeleport?.TeleportPlayer();
        }
        wasInFactoryLastFrame = this.isInFactory;
        if (GameNetworkManager.Instance.localPlayerController == player)
        {
            meshRenderer.enabled = false;
            scanNodeProperties.enabled = false;
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
        else
        {
            player.transform.position = playerBone.position;
            player.transform.forward = this.transform.forward;
            Quaternion rotation = transform.rotation;
            rotation *= Quaternion.Euler(nonHeldRotationOffset);
            player.transform.rotation = rotation;
        }

        if (localHeadRenderer == null)
        {
            localHeadRenderer = player.localVisor.GetComponentInChildren<Renderer>();
        }
        localHeadRenderer.enabled = false;

        int alivePlayers = StartOfRound.Instance.allPlayerScripts.Count(player => player.isPlayerControlled && !player.isPlayerDead && !player.IsPseudoDead());
        if (StartOfRound.Instance.shipIsLeaving || StartOfRound.Instance.allPlayerScripts.Count(player => player.isPlayerControlled && !player.isPlayerDead && !player.IsPseudoDead()) == 0)
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
            if (localHeadRenderer != null)
            {
                localHeadRenderer.enabled = true;
            }
            player.SetPseudoDead(false);

            player.gameObject.layer = 3;
            if (GameNetworkManager.Instance.localPlayerController == player)
            {
                scanNodeProperties.enabled = true;
                meshRenderer.enabled = true;
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