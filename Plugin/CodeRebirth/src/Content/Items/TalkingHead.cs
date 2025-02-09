using System.Linq;
using CodeRebirth.src.Util;
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

    public override void Update()
    {
        base.Update();
        if (player == null) return;
        if (isInFactory != wasInFactoryLastFrame && GameNetworkManager.Instance.localPlayerController == player)
        {
            Plugin.ExtendedLogging("Teleporting player.");
            var entranceTeleport = CodeRebirthUtils.entrancePoints.OrderBy(p => Vector3.Distance(p.entrancePoint.position, player.transform.position)).FirstOrDefault();
            entranceTeleport.TeleportPlayer();
        }
        wasInFactoryLastFrame = this.isInFactory;
        if (GameNetworkManager.Instance.localPlayerController == player) meshRenderer.enabled = false;
        player.disableMoveInput = true;
        player.transform.position = playerBone.position;
        player.transform.rotation = transform.rotation;
        if (StartOfRound.Instance.shipIsLeaving || StartOfRound.Instance.allPlayerScripts.Where(p => p.isPlayerControlled && !p.isPlayerDead).Count() <= 0)
        {
            player.DisablePlayerModel(player.gameObject, true, false);
            player.disableMoveInput = false;
            player.disableInteract = false;
            player.thisPlayerModelLOD2.gameObject.SetActive(true);
            player.thisPlayerModelLOD1.gameObject.SetActive(true);
            player.headCostumeContainer.gameObject.SetActive(true);
            player.headCostumeContainerLocal.gameObject.SetActive(true);
            player.isPlayerDead = false;
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
    public void SyncTalkingHeadServerRpc(int playerIndex)
    {
        SyncTalkingHeadClientRpc(playerIndex);
    }

    [ClientRpc]
    private void SyncTalkingHeadClientRpc(int playerIndex)
    {
        player = StartOfRound.Instance.allPlayerScripts[playerIndex];
        if (GameNetworkManager.Instance.localPlayerController == player)
        {
            meshRenderer.enabled = false;
        }
    }
}